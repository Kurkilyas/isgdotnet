using System.Security.Cryptography;
using System.Text;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Helpers;
using InvoiceTrackingSystemBackend.Interfaces;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using InvoiceTrackingSystemBackend.Settings;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Auth;

public class AuthService : IAuthService
{
    private const int CodeLength = 6;
    private const int CodeExpireMinutes = 10;
    private const int MaxCodeAttempts = 5;

    private const int MaxFailedLogins = 5;
    private const int LockoutMinutes = 15;
    private const string VerificationLockMessage =
        "Doğrulama deneme hakkı dolduğu için hesabınız kilitlendi. Sistem yöneticinizle iletişime geçin.";

    private readonly UserDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IUserActivityLogService _activityLogService;
    private readonly IJwtHelper _jwtHelper;
    private readonly JwtOptions _jwt;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _env;

    public AuthService(
        UserDbContext context,
        IEmailService emailService,
        IUserActivityLogService activityLogService,
        IJwtHelper jwtHelper,
        IOptions<JwtOptions> jwt,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment env)
    {
        _context = context;
        _emailService = emailService;
        _activityLogService = activityLogService;
        _jwtHelper = jwtHelper;
        _jwt = jwt.Value;
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (existing is not null && existing.IsVerified)
        {
            throw new ConflictException("Bu e-posta adresi zaten kayıtlı.");
        }

        if (existing is not null && !existing.IsActive)
        {
            throw new ForbiddenException(
                "Hesabınız kilitli. Yeni doğrulama kodu gönderilemez. Sistem yöneticinizle iletişime geçin.");
        }

        User user;
        if (existing is not null)
        {
            user = existing;
            user.FullName = request.FullName.Trim();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
          
            user.UpdatedAt = DateTime.UtcNow;
            user.PasswordChangedAt = DateTime.UtcNow;
        }
        else
        {
            user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
           
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                PasswordChangedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        var code = await IssueEmailVerificationCodeAsync(user);

        await _activityLogService.LogAsync(user.Id, AuthActivityType.REGISTERED, "Kullanıcı kaydı oluşturuldu.");

        await _emailService.SendAsync(
            EmailType.EmailVerification,
            [user.Email],
            placeholders: new Dictionary<string, string>
            {
                ["FullName"] = user.FullName,
                ["Code"] = code,
                ["ExpireMinutes"] = CodeExpireMinutes.ToString()
            });

        return new RegisterResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            IsVerified = user.IsVerified,
            Message = "Doğrulama kodu e-posta adresinize gönderildi.",
            DevelopmentCode = _env.IsDevelopment() ? code : null
        };
    }

    public async Task<VerifyEmailResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        if (user.IsVerified)
        {
            throw new BadRequestException("E-posta adresi zaten doğrulanmış.");
        }

        EnsureAccountNotLocked(user);

        var now = DateTime.UtcNow;
        var verification = await _context.UserVerificationCodes
            .Where(c =>
                c.UserId == user.Id &&
                c.Purpose == VerificationPurpose.EMAIL_VERIFY &&
                c.UsedAt == null &&
                c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification is null)
        {
            throw new BadRequestException("Geçerli bir doğrulama kodu bulunamadı. Lütfen yeni kod isteyin.");
        }

        if (verification.AttemptCount >= MaxCodeAttempts)
        {
            await LockAccountAfterVerificationAbuseAsync(user, verification);
            throw new ForbiddenException(VerificationLockMessage);
        }

        if (!FixedTimeEquals(verification.CodeHash, HashCode(request.Code.Trim())))
        {
            verification.AttemptCount++;
            if (verification.AttemptCount >= MaxCodeAttempts)
            {
                await LockAccountAfterVerificationAbuseAsync(user, verification);
                throw new ForbiddenException(VerificationLockMessage);
            }

            await _context.SaveChangesAsync();
            throw new BadRequestException("Doğrulama kodu hatalı.");
        }

        verification.UsedAt = now;
        user.IsVerified = true;
        user.UpdatedAt = now;

        await _activityLogService.LogAsync(user.Id, AuthActivityType.EMAIL_VERIFIED, "E-posta doğrulandı.");

        return new VerifyEmailResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            IsVerified = user.IsVerified
        };
    }

    public async Task<RegisterResponseDto> ResendEmailVerificationAsync(ResendVerificationRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        if (user.IsVerified)
        {
            throw new BadRequestException("E-posta adresi zaten doğrulanmış.");
        }

        EnsureAccountNotLocked(user);

        var now = DateTime.UtcNow;
        var activeCode = await _context.UserVerificationCodes
            .Where(c =>
                c.UserId == user.Id &&
                c.Purpose == VerificationPurpose.EMAIL_VERIFY &&
                c.UsedAt == null &&
                c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeCode is not null)
        {
            if (activeCode.AttemptCount >= MaxCodeAttempts)
            {
                await LockAccountAfterVerificationAbuseAsync(user, activeCode);
                throw new ForbiddenException(VerificationLockMessage);
            }

            var remainingMinutes = Math.Max(1, (int)Math.Ceiling((activeCode.ExpiresAt - now).TotalMinutes));
            throw new BadRequestException(
                $"Aktif bir doğrulama kodunuz zaten var. Yeni kod istemek için {remainingMinutes} dakika bekleyin.");
        }

        var code = await IssueEmailVerificationCodeAsync(user);

        await _emailService.SendAsync(
            EmailType.EmailVerification,
            [user.Email],
            placeholders: new Dictionary<string, string>
            {
                ["FullName"] = user.FullName,
                ["Code"] = code,
                ["ExpireMinutes"] = CodeExpireMinutes.ToString()
            });

        return new RegisterResponseDto
        {
            Id = user.Id,
            Email = user.Email,
            IsVerified = user.IsVerified,
            Message = "Doğrulama kodu e-posta adresinize tekrar gönderildi.",
            DevelopmentCode = _env.IsDevelopment() ? code : null
        };
    }

    public async Task<AuthSessionResult> LoginAsync(LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        var now = DateTime.UtcNow;

        if (user is null)
        {
            await _activityLogService.LogAsync(null, AuthActivityType.LOGIN_FAILED, $"Bilinmeyen e-posta: {email}");
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > now)
        {
            throw new UnauthorizedException("Hesap geçici olarak kilitli. Lütfen daha sonra tekrar deneyin veya Sistem Yöneticinize başvurun.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Hesap pasif durumda. Lütfen Sistem Yöneticinize başvurun.");
        }

        if (!user.IsVerified)
        {
            throw new BadRequestException("E-posta adresi henüz doğrulanmamış.");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockedUntil = now.AddMinutes(LockoutMinutes);
                user.FailedLoginCount = 0;
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync(user.Id, AuthActivityType.ACCOUNT_LOCKED, "Art arda hatalı giriş.");
                throw new UnauthorizedException("Hesap geçici olarak kilitlendi.");
            }

            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(user.Id, AuthActivityType.LOGIN_FAILED, "Hatalı şifre.");
            throw new UnauthorizedException("E-posta veya şifre hatalı.");
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = now;
        user.UpdatedAt = now;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(user.Id, AuthActivityType.LOGIN_SUCCESS, "Giriş başarılı.");

        return await CreateSessionAsync(user);
    }

    public async Task<AuthSessionResult> RefreshAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedException("Oturum yenilenemedi.");
        }

        var tokenHash = _jwtHelper.HashToken(refreshToken);
        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (existing is null)
        {
            throw new UnauthorizedException("Oturum yenilenemedi.");
        }

        var now = DateTime.UtcNow;
        if (existing.RevokedAt is not null)
        {
            await RevokeAllUserTokensAsync(existing.UserId, now);
            throw new UnauthorizedException("Oturum geçersiz. Lütfen tekrar giriş yapın.");
        }

        if (existing.ExpiresAt <= now)
        {
            throw new UnauthorizedException("Oturum süresi doldu. Lütfen tekrar giriş yapın.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId);
        if (user is null || !user.IsActive || !user.IsVerified)
        {
            throw new UnauthorizedException("Oturum geçersiz.");
        }

        var session = await CreateSessionAsync(user);
        existing.RevokedAt = now;
        existing.ReplacedByTokenId = await _context.RefreshTokens
            .Where(t => t.TokenHash == _jwtHelper.HashToken(session.RefreshToken))
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
        await _context.SaveChangesAsync();

        return session;
    }

    public async Task LogoutAsync(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = _jwtHelper.HashToken(refreshToken);
        var existing = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null);

        if (existing is null)
        {
            return;
        }

        existing.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(existing.UserId, AuthActivityType.LOGOUT, "Çıkış yapıldı.");
    }

    private async Task<string> IssueEmailVerificationCodeAsync(User user)
    {
        var now = DateTime.UtcNow;
        var activeCodes = await _context.UserVerificationCodes
            .Where(c =>
                c.UserId == user.Id &&
                c.Purpose == VerificationPurpose.EMAIL_VERIFY &&
                c.UsedAt == null &&
                c.DeletedAt == null)
            .ToListAsync();

        foreach (var old in activeCodes)
        {
            old.DeletedAt = now;
        }

        var code = RandomNumberGenerator.GetInt32(0, (int)Math.Pow(10, CodeLength))
            .ToString($"D{CodeLength}");

        _context.UserVerificationCodes.Add(new UserVerificationCode
        {
            UserId = user.Id,
            CodeHash = HashCode(code),
            Purpose = VerificationPurpose.EMAIL_VERIFY,
            ExpiresAt = now.AddMinutes(CodeExpireMinutes),
            AttemptCount = 0,
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return code;
    }

    private async Task<AuthSessionResult> CreateSessionAsync(User user)
    {
        var (accessToken, refreshToken, accessExpiresAt) = await _jwtHelper.GenerateTokensAsync(user);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenExpiryDays);
        var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwtHelper.HashToken(refreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedByIp = ip,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new AuthSessionResult
        {
            RefreshToken = refreshToken,
            RefreshExpiresAt = refreshExpiresAt,
            Response = new AuthResponseDto
            {
                AccessToken = accessToken,
                AccessExpiresAt = accessExpiresAt,
                User = await MapUserAsync(user)
            }
        };
    }

    private async Task RevokeAllUserTokensAsync(int userId, DateTime now)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<UserListDto> MapUserAsync(User user)
    {
        var department = await UserPositionHelper.ResolveDepartmentAsync(_context, user.Id);
        return new UserListDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            DepartmentId = department?.Id,
            DepartmentName = department?.Name,
            Phone = user.Phone,
            Position = await UserPositionHelper.ResolveAsync(_context, user.Id),
            IsOutOfOffice = user.IsOutOfOffice,
            OutOfOfficeUntil = user.OutOfOfficeUntil,
            CreatedAt = user.CreatedAt
        };
    }

    private static void EnsureAccountNotLocked(User user)
    {
        if (!user.IsActive)
        {
            throw new ForbiddenException(VerificationLockMessage);
        }
    }

    private async Task LockAccountAfterVerificationAbuseAsync(User user, UserVerificationCode code)
    {
        var now = DateTime.UtcNow;
        code.DeletedAt = now;
        user.IsActive = false;
        user.UpdatedAt = now;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            user.Id,
            AuthActivityType.ACCOUNT_LOCKED,
            "E-posta doğrulama deneme hakkı doldu (5 hatalı deneme).");
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
               CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
