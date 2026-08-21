using System.Security.Cryptography;
using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using InvoiceTrackingSystemBackend.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InvoiceTrackingSystemBackend.Services.Auth;

public class UserService : IUserService
{
    private static readonly Dictionary<string, string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp"
    };

    private readonly UserDbContext _context;
    private readonly IStorageService _storage;
    private readonly IUserActivityLogService _activityLogService;
    private readonly StorageOptions _storageOptions;

    public UserService(
        UserDbContext context,
        IStorageService storage,
        IUserActivityLogService activityLogService,
        IOptions<StorageOptions> storageOptions)
    {
        _context = context;
        _storage = storage;
        _activityLogService = activityLogService;
        _storageOptions = storageOptions.Value;
    }

    public async Task<PagedResult<UserListDto>> GetListAsync(int actorUserId, int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var scope = await ResolveUserDirectoryScopeAsync(actorUserId);
        var query = _context.Users.AsNoTracking();

        if (scope.DepartmentId.HasValue)
        {
            query = query.Where(u => u.DepartmentId == scope.DepartmentId.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = users.Select(MapList).ToList();

        return PagedResult<UserListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<UserListDto?> GetByIdAsync(int actorUserId, int id)
    {
        var scope = await ResolveUserDirectoryScopeAsync(actorUserId);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return null;
        }

        if (scope.DepartmentId.HasValue && user.DepartmentId != scope.DepartmentId.Value)
        {
            throw new ForbiddenException("Bu kullanıcıyı görüntüleme yetkiniz yok.");
        }

        return MapList(user);
    }

    /// <summary>
    /// UserRead: tüm kullanıcılar (DepartmentId = null).
    /// IsManager: yalnızca kendi departmanı.
    /// </summary>
    private async Task<UserDirectoryScope> ResolveUserDirectoryScopeAsync(int actorUserId)
    {
        var now = DateTime.UtcNow;
        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == actorUserId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive)
            .Select(ur => new
            {
                ur.Role.IsManager,
                ur.Role.DepartmentId,
                PermissionNames = ur.Role.RolePermissions
                    .Where(rp => rp.Permission.IsActive)
                    .Select(rp => rp.Permission.Name)
            })
            .ToListAsync();

        var hasUserRead = roles
            .SelectMany(r => r.PermissionNames)
            .Any(name => name == "UserRead");

        if (hasUserRead)
        {
            return new UserDirectoryScope(null);
        }

        if (!roles.Any(r => r.IsManager))
        {
            throw new ForbiddenException("Kullanıcı listesini görüntüleme yetkiniz yok.");
        }

        var actorDepartmentId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == actorUserId)
            .Select(u => u.DepartmentId)
            .FirstOrDefaultAsync();

        var departmentId = actorDepartmentId
            ?? roles.Where(r => r.IsManager).Select(r => r.DepartmentId).FirstOrDefault(id => id.HasValue);

        if (!departmentId.HasValue)
        {
            throw new ForbiddenException("Müdür hesabına departman atanmadığı için kullanıcı listesi görüntülenemez.");
        }

        return new UserDirectoryScope(departmentId);
    }

    private sealed record UserDirectoryScope(int? DepartmentId);

    public async Task<UserMeDto> GetMeAsync(int userId)
    {
        var user = await GetRequiredUserAsync(userId);
        return await MapMeAsync(user);
    }

    public async Task<UserMeDto> UpdateMeAsync(int userId, UpdateMeRequestDto request)
    {
        var user = await GetRequiredUserAsync(userId);
        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.Position = string.IsNullOrWhiteSpace(request.Position) ? null : request.Position.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(user.Id, AuthActivityType.PROFILE_UPDATED, "Profil bilgileri güncellendi.");
        return await MapMeAsync(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        var user = await GetRequiredUserAsync(userId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BadRequestException("Mevcut şifre hatalı.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new BadRequestException("Yeni şifre mevcut şifre ile aynı olamaz.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(user.Id, AuthActivityType.PASSWORD_CHANGED, "Kullanıcı şifresini değiştirdi.");
    }

    public async Task<UserMeDto> UpdateOutOfOfficeAsync(int userId, UpdateOutOfOfficeRequestDto request)
    {
        var user = await GetRequiredUserAsync(userId);
        user.IsOutOfOffice = request.IsOutOfOffice;
        user.OutOfOfficeUntil = request.IsOutOfOffice ? request.OutOfOfficeUntil : null;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        var status = user.IsOutOfOffice
            ? $"Kullanıcı izinli olarak işaretlendi{(user.OutOfOfficeUntil.HasValue ? $" (bitiş: {user.OutOfOfficeUntil:u})" : "")}."
            : "Kullanıcı izinli işareti kaldırıldı.";
        await _activityLogService.LogAsync(user.Id, AuthActivityType.OUT_OF_OFFICE_CHANGED, status);
        return await MapMeAsync(user);
    }

    public async Task<UserFileMetaDto> UploadMyFileAsync(int userId, UserFileKind fileKind, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new BadRequestException("Dosya seçilmedi.");
        }

        if (file.Length > _storageOptions.MaxFileSizeBytes)
        {
            throw new BadRequestException($"Dosya boyutu en fazla {_storageOptions.MaxFileSizeBytes / (1024 * 1024)} MB olabilir.");
        }

        if (!AllowedContentTypes.TryGetValue(file.ContentType, out var extension))
        {
            throw new BadRequestException("Yalnızca PNG, JPEG veya WebP görselleri yüklenebilir.");
        }

        var user = await GetRequiredUserAsync(userId);
        var folder = fileKind == UserFileKind.SIGNATURE ? "signatures" : "photos";
        var relativePath = $"users/{folder}/{userId}/{Guid.NewGuid():N}{extension}";

        string checksum;
        await using (var hashStream = file.OpenReadStream())
        {
            checksum = Convert.ToHexString(await SHA256.HashDataAsync(hashStream));
        }

        await using (var saveStream = file.OpenReadStream())
        {
            await _storage.SaveAsync(relativePath, saveStream);
        }

        var now = DateTime.UtcNow;
        var currentFiles = await _context.UserFiles
            .Where(f => f.UserId == userId && f.FileKind == fileKind && f.IsCurrent)
            .ToListAsync();

        foreach (var old in currentFiles)
        {
            old.IsCurrent = false;
            old.UpdatedAt = now;
        }

        var entity = new UserFile
        {
            UserId = userId,
            FileKind = fileKind,
            OriginalFileName = Path.GetFileName(file.FileName),
            NasRelativePath = relativePath,
            ContentType = file.ContentType,
            FileSizeBytes = (int)file.Length,
            ChecksumSha256 = checksum,
            IsCurrent = true,
            UploadedByUserId = userId,
            CreatedAt = now
        };
        _context.UserFiles.Add(entity);
        user.UpdatedAt = now;
        await _context.SaveChangesAsync();
        var kindLabel = fileKind == UserFileKind.SIGNATURE ? "imza" : "profil fotoğrafı";
        await _activityLogService.LogAsync(user.Id, AuthActivityType.FILE_UPLOADED, $"{kindLabel} yüklendi.");

        return MapFile(entity);
    }

    public async Task<UserFileMetaDto> GetMyFileMetaAsync(int userId, UserFileKind fileKind)
    {
        var file = await GetCurrentFileAsync(userId, fileKind);
        return MapFile(file);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenMyFileAsync(int userId, UserFileKind fileKind)
    {
        var file = await GetCurrentFileAsync(userId, fileKind);
        var stream = await _storage.OpenReadAsync(file.NasRelativePath);
        return (stream, file.ContentType, file.OriginalFileName);
    }

    private async Task<UserFile> GetCurrentFileAsync(int userId, UserFileKind fileKind)
    {
        var file = await _context.UserFiles
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.FileKind == fileKind && f.IsCurrent)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();

        if (file is null)
        {
            throw new NotFoundException(
                fileKind == UserFileKind.SIGNATURE ? "İmza dosyası bulunamadı." : "Profil fotoğrafı bulunamadı.");
        }

        return file;
    }

    private async Task<User> GetRequiredUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        return user;
    }

    private async Task<UserMeDto> MapMeAsync(User user)
    {
        var kinds = await _context.UserFiles
            .AsNoTracking()
            .Where(f => f.UserId == user.Id && f.IsCurrent)
            .Select(f => f.FileKind)
            .ToListAsync();

        return new UserMeDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            DepartmentId = user.DepartmentId,
            Phone = user.Phone,
            Position = user.Position,
            IsOutOfOffice = user.IsOutOfOffice,
            OutOfOfficeUntil = user.OutOfOfficeUntil,
            HasProfilePhoto = kinds.Contains(UserFileKind.PROFILE_PHOTO),
            HasSignature = kinds.Contains(UserFileKind.SIGNATURE),
            CreatedAt = user.CreatedAt
        };
    }

    private static UserListDto MapList(User user)
    {
        return new UserListDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            IsActive = user.IsActive,
            IsVerified = user.IsVerified,
            DepartmentId = user.DepartmentId,
            Phone = user.Phone,
            Position = user.Position,
            CreatedAt = user.CreatedAt
        };
    }

    private static UserFileMetaDto MapFile(UserFile file)
    {
        return new UserFileMetaDto
        {
            FileKind = file.FileKind,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.FileSizeBytes,
            UploadedAt = file.CreatedAt
        };
    }
}
