using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Auth;

public class PermissionService : IPermissionService
{
    private readonly UserDbContext _context;
    private readonly IUserActivityLogService _activityLogService;

    public PermissionService(UserDbContext context, IUserActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<PagedResult<PermissionListDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.DisplayName.Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var permissions = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = permissions.Select(MapPermission).ToList();
        return PagedResult<PermissionListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        var query = _context.Permissions.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            query = query.Where(p => p.DisplayName.Contains(term));
        }

        return await query
            .OrderBy(p => p.DisplayName)
            .Select(p => new IdNameDto
            {
                Id = p.Id,
                Name = p.DisplayName
            })
            .ToListAsync();
    }

    public async Task<PermissionListDto> GetByIdAsync(int id)
    {
        var permission = await _context.Permissions.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (permission is null)
        {
            throw new NotFoundException("İzin bulunamadı.");
        }

        return MapPermission(permission);
    }

    public async Task<PermissionListDto> CreateAsync(int actorUserId, CreatePermissionRequestDto request)
    {
        var name = NormalizePermissionName(request.Name);
        await EnsurePermissionNameAvailableAsync(name);

        var now = DateTime.UtcNow;
        var permission = new Permission
        {
            Name = name,
            DisplayName = request.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = now
        };
        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.PERMISSION_CREATED,
            $"İzin oluşturuldu: {permission.Name}.");

        return MapPermission(permission);
    }

    public async Task<PermissionListDto> UpdateAsync(int actorUserId, int id, UpdatePermissionRequestDto request)
    {
        var permission = await GetRequiredPermissionAsync(id);
        var name = NormalizePermissionName(request.Name);
        await EnsurePermissionNameAvailableAsync(name, excludePermissionId: id);

        permission.Name = name;
        permission.DisplayName = request.DisplayName.Trim();
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.PERMISSION_UPDATED,
            $"İzin güncellendi: {permission.Name}.");

        return MapPermission(permission);
    }

    public async Task<PermissionListDto> SetActiveAsync(int actorUserId, int id, SetPermissionActiveRequestDto request)
    {
        var permission = await GetRequiredPermissionAsync(id);
        if (permission.IsActive == request.IsActive)
        {
            return MapPermission(permission);
        }

        permission.IsActive = request.IsActive;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            request.IsActive ? AuthActivityType.PERMISSION_UPDATED : AuthActivityType.PERMISSION_DEACTIVATED,
            request.IsActive
                ? $"İzin yeniden aktif edildi: {permission.Name}."
                : $"İzin pasife alındı: {permission.Name}.");

        return MapPermission(permission);
    }

    public async Task DeleteAsync(int actorUserId, int id)
    {
        var permission = await GetRequiredPermissionAsync(id);
        var now = DateTime.UtcNow;
        permission.IsActive = false;
        permission.DeletedAt = now;

        var assignments = await _context.RolePermissions
            .Where(rp => rp.PermissionId == id)
            .ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.DeletedAt = now;
        }

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.PERMISSION_DELETED,
            $"İzin silindi: {permission.Name}.");
    }

    public async Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId)
    {
        await EnsureRoleExistsAsync(roleId);

        var assignments = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId && rp.Permission.IsActive)
            .OrderBy(rp => rp.Permission.Name)
            .Select(rp => new RolePermissionDto
            {
                PermissionId = rp.PermissionId,
                Name = rp.Permission.Name,
                DisplayName = rp.Permission.DisplayName,
                CreatedAt = rp.CreatedAt,
                GrantedBy = rp.GrantedBy
            })
            .ToListAsync();

        return assignments;
    }

    public async Task<IReadOnlyList<RolePermissionDto>> AssignToRoleAsync(
        int actorUserId,
        int roleId,
        AssignPermissionRequestDto request)
    {
        var role = await GetRequiredRoleAsync(roleId);
        if (!role.IsActive)
        {
            throw new BadRequestException("Pasif bir role izin atanamaz.");
        }

        var permissionIds = request.PermissionIds.Distinct().ToList();
        if (permissionIds.Count == 0)
        {
            throw new BadRequestException("En az bir izin seçilmelidir.");
        }

        var permissions = await _context.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync();

        var missing = permissionIds.Except(permissions.Select(p => p.Id)).ToList();
        if (missing.Count > 0)
        {
            throw new NotFoundException($"İzin bulunamadı: {string.Join(", ", missing)}");
        }

        var inactive = permissions.Where(p => !p.IsActive).Select(p => p.Name).ToList();
        if (inactive.Count > 0)
        {
            throw new BadRequestException($"Pasif izin atanamaz: {string.Join(", ", inactive)}");
        }

        var existingRows = await _context.RolePermissions
            .IgnoreQueryFilters()
            .Where(rp => rp.RoleId == roleId && permissionIds.Contains(rp.PermissionId))
            .ToListAsync();

        var now = DateTime.UtcNow;
        var assignedNames = new List<string>();

        foreach (var permission in permissions)
        {
            var existing = existingRows.FirstOrDefault(rp => rp.PermissionId == permission.Id);
            if (existing is not null && existing.DeletedAt is null)
            {
                continue;
            }

            if (existing is not null)
            {
                existing.DeletedAt = null;
                existing.GrantedBy = actorUserId;
                existing.CreatedAt = now;
            }
            else
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    GrantedBy = actorUserId,
                    CreatedAt = now
                });
            }

            assignedNames.Add(permission.Name);
        }

        if (assignedNames.Count > 0)
        {
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync(
                actorUserId,
                AuthActivityType.PERMISSION_ASSIGNED,
                $"İzinler atandı: {string.Join(", ", assignedNames)} → {role.Name}.");
        }

        return await GetRolePermissionsAsync(roleId);
    }

    public async Task RevokeFromRoleAsync(int actorUserId, int roleId, int permissionId)
    {
        await EnsureRoleExistsAsync(roleId);

        var assignment = await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Include(rp => rp.Role)
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (assignment is null)
        {
            throw new NotFoundException("Rolde bu izin bulunamadı.");
        }

        assignment.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.PERMISSION_REVOKED,
            $"İzin alındı: {assignment.Permission.Name} ← {assignment.Role.Name}.");
    }

    private async Task<Permission> GetRequiredPermissionAsync(int id)
    {
        var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);
        if (permission is null)
        {
            throw new NotFoundException("İzin bulunamadı.");
        }

        return permission;
    }

    private async Task<Role> GetRequiredRoleAsync(int roleId)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }

        return role;
    }

    private async Task EnsureRoleExistsAsync(int roleId)
    {
        var exists = await _context.Roles.AsNoTracking().AnyAsync(r => r.Id == roleId);
        if (!exists)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }
    }

    private async Task EnsurePermissionNameAvailableAsync(string name, int? excludePermissionId = null)
    {
        var exists = await _context.Permissions.AnyAsync(p =>
            p.Name == name && (!excludePermissionId.HasValue || p.Id != excludePermissionId.Value));
        if (exists)
        {
            throw new ConflictException("Bu izin adı zaten kullanılıyor.");
        }
    }

    private static string NormalizePermissionName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("İzin adı boş olamaz.");
        }

        return trimmed;
    }

    private static PermissionListDto MapPermission(Permission permission)
    {
        return new PermissionListDto
        {
            Id = permission.Id,
            Name = permission.Name,
            DisplayName = permission.DisplayName,
            IsActive = permission.IsActive,
            CreatedAt = permission.CreatedAt
        };
    }
}
