using System.Security.Cryptography;
using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Helpers;
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

        var scope = await GetActorScopeAsync(actorUserId);
        var query = _context.Users.AsNoTracking();
        if (scope.DepartmentId.HasValue)
        {
            var now = DateTime.UtcNow;
            var userIdsInDepartment = _context.UserRoles
                .AsNoTracking()
                .Where(ur =>
                    ur.IsActive &&
                    (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                    ur.Role.IsActive &&
                    ur.Role.DepartmentId == scope.DepartmentId.Value)
                .Select(ur => ur.UserId);

            query = query.Where(u => userIdsInDepartment.Contains(u.Id));
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var positions = await UserPositionHelper.ResolveManyAsync(_context, users.Select(u => u.Id).ToList());
        var departments = await UserPositionHelper.ResolveDepartmentsAsync(_context, users.Select(u => u.Id).ToList());
        var items = users.Select(u => MapList(u, positions.GetValueOrDefault(u.Id), departments.GetValueOrDefault(u.Id))).ToList();

        return PagedResult<UserListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null)
    {
        var query = _context.Users.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new IdNameDto
            {
                Id = u.Id,
                Name = u.FullName
            })
            .ToListAsync();
    }

    public async Task<UserListDto?> GetByIdAsync(int actorUserId, int id)
    {
        var scope = await GetActorScopeAsync(actorUserId);
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
        {
            return null;
        }

        await EnsureUserInScopeAsync(user.Id, scope, "Bu kullanıcıyı görüntüleme yetkiniz yok.");

        return await MapListAsync(user);
    }

    public async Task<UserListDto> UpdateUserAsync(int actorUserId, int id, UpdateUserRequestDto request)
    {
        var scope = await GetActorScopeAsync(actorUserId);
        var user = await GetRequiredUserAsync(id);
        await EnsureUserInScopeAsync(user.Id, scope, "Bu kullanıcıyı güncelleme yetkiniz yok.");

        user.FullName = request.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.IsOutOfOffice = request.IsOutOfOffice;
        user.OutOfOfficeUntil = request.IsOutOfOffice ? request.OutOfOfficeUntil : null;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            user.Id,
            AuthActivityType.USER_UPDATED,
            $"Kullanıcı bilgileri güncellendi (işlemi yapan: {actorUserId}).");

        return await MapListAsync(user);
    }

    public async Task<UserListDto> SetUserActiveAsync(int actorUserId, int id, SetUserActiveRequestDto request)
    {
        if (actorUserId == id)
        {
            throw new ForbiddenException("Kendi hesabınızın durumunu bu endpoint ile değiştiremezsiniz.");
        }

        var user = await GetManagedUserAsync(actorUserId, id, "Bu kullanıcının durumunu değiştirme yetkiniz yok.");

        if (user.IsActive == request.IsActive)
        {
            return await MapListAsync(user);
        }

        var now = DateTime.UtcNow;
        user.IsActive = request.IsActive;
        user.UpdatedAt = now;

        if (request.IsActive)
        {
            user.FailedLoginCount = 0;
            user.LockedUntil = null;
        }
        else
        {
            var tokens = await _context.RefreshTokens
                .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                .ToListAsync();
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }
        }

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            user.Id,
            request.IsActive ? AuthActivityType.ACCOUNT_UNLOCKED : AuthActivityType.USER_DEACTIVATED,
            request.IsActive
                ? $"Hesap yeniden aktif edildi (işlemi yapan: {actorUserId})."
                : $"Hesap pasife alındı (işlemi yapan: {actorUserId}).");

        return await MapListAsync(user);
    }

    /// <summary>
    /// Kapsam rol bazlı belirlenir:
    /// - Aktif rollerinden herhangi biri müdür değilse ve UserRead/UserWrite izni taşıyorsa → Global (admin).
    /// - Aksi halde aktif bir müdür (IsManager) rolü varsa → o rolün DepartmentId'si.
    /// - Hiçbiri yoksa → Global (Permission attribute burada zaten erişimi engellemiş olur).
    /// </summary>
    private async Task<ActorScope> GetActorScopeAsync(int actorUserId)
    {
        var now = DateTime.UtcNow;

        var activeRoles = await _context.UserRoles
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
                Permissions = ur.Role.RolePermissions.Select(rp => rp.Permission.Name)
            })
            .ToListAsync();

        var hasGlobalAccess = activeRoles.Any(r =>
            !r.IsManager &&
            r.Permissions.Any(p => p == "UserRead" || p == "UserWrite"));

        if (hasGlobalAccess)
        {
            return ActorScope.Global();
        }

        var managerRole = activeRoles.FirstOrDefault(r => r.IsManager);
        if (managerRole?.DepartmentId is int departmentId)
        {
            return ActorScope.Department(departmentId);
        }

        return ActorScope.Global();
    }

    private async Task<User> GetManagedUserAsync(
        int actorUserId,
        int targetUserId,
        string forbiddenMessage)
    {
        var scope = await GetActorScopeAsync(actorUserId);
        var user = await GetRequiredUserAsync(targetUserId);
        await EnsureUserInScopeAsync(user.Id, scope, forbiddenMessage);
        return user;
    }

    /// <summary>
    /// Hedef kullanıcının departmanı, kendi aktif rollerinin bağlı olduğu departman(lar)dan belirlenir.
    /// </summary>
    private async Task EnsureUserInScopeAsync(int targetUserId, ActorScope scope, string forbiddenMessage)
    {
        if (!scope.DepartmentId.HasValue)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var isInDepartment = await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur =>
                ur.UserId == targetUserId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive &&
                ur.Role.DepartmentId == scope.DepartmentId.Value);

        if (!isInDepartment)
        {
            throw new ForbiddenException(forbiddenMessage);
        }
    }

    private sealed record ActorScope(int? DepartmentId)
    {
        public static ActorScope Global() => new((int?)null);
        public static ActorScope Department(int departmentId) => new(departmentId);
    }

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
        var relativePath = $"users/{folder}/{user.FullName.Replace(" ", "_")}/{Guid.NewGuid():N}{extension}";

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

    public async Task<IReadOnlyList<UserFileMetaDto>> GetMyFilesAsync(int userId)
    {
        var files = await _context.UserFiles
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.IsCurrent)
            .OrderBy(f => f.FileKind)
            .ToListAsync();

        return files.Select(MapFile).ToList();
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

    public async Task DeleteMyFileAsync(int userId, UserFileKind fileKind)
    {
        var file = await _context.UserFiles
            .Where(f => f.UserId == userId && f.FileKind == fileKind && f.IsCurrent)
            .OrderByDescending(f => f.CreatedAt)
            .FirstOrDefaultAsync();
        if (file is null)
        {
            throw new NotFoundException(
                fileKind == UserFileKind.SIGNATURE ? "İmza dosyası bulunamadı." : "Profil fotoğrafı bulunamadı.");
        }

        var now = DateTime.UtcNow;
        file.IsCurrent = false;
        file.DeletedAt = now;
        file.UpdatedAt = now;
        await _context.SaveChangesAsync();
        var kindLabel = fileKind == UserFileKind.SIGNATURE ? "imza" : "profil fotoğrafı";
        await _activityLogService.LogAsync(userId, AuthActivityType.FILE_DELETED, $"{kindLabel} silindi.");
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

        var department = await UserPositionHelper.ResolveDepartmentAsync(_context, user.Id);

        return new UserMeDto
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
            HasProfilePhoto = kinds.Contains(UserFileKind.PROFILE_PHOTO),
            HasSignature = kinds.Contains(UserFileKind.SIGNATURE),
            CreatedAt = user.CreatedAt
        };
    }

    private async Task<UserListDto> MapListAsync(User user)
    {
        return MapList(
            user,
            await UserPositionHelper.ResolveAsync(_context, user.Id),
            await UserPositionHelper.ResolveDepartmentAsync(_context, user.Id));
    }

    private static UserListDto MapList(User user, string? position, UserDepartmentInfo? department)
    {
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
            Position = position,
            IsOutOfOffice = user.IsOutOfOffice,
            OutOfOfficeUntil = user.OutOfOfficeUntil,
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
