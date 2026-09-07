using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.Entities.Auth;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Auth;

public class RoleService : IRoleService
{
    private readonly UserDbContext _context;
    private readonly IUserActivityLogService _activityLogService;

    public RoleService(UserDbContext context, IUserActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<PagedResult<RoleListDto>> GetListAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Roles
            .AsNoTracking()
            .Include(r => r.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                r.Name.Contains(term) ||
                r.DisplayName.Contains(term) ||
                (r.Department != null && r.Department.Name.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var roles = await query
            .OrderBy(r => r.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = roles.Select(MapRole).ToList();
        return PagedResult<RoleListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null)
    {
        var query = _context.Roles.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(r => r.DisplayName)
            .Select(r => new IdNameDto
            {
                Id = r.Id,
                Name = r.DisplayName
            })
            .ToListAsync();
    }

    public async Task<RoleListDto> GetByIdAsync(int id)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Department)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }

        return MapRole(role);
    }

    public async Task<RoleListDto> CreateAsync(int actorUserId, CreateRoleRequestDto request)
    {
        var name = NormalizeRoleName(request.Name);
        await EnsureRoleNameAvailableAsync(name);
        await EnsureDepartmentExistsAsync(request.DepartmentId);

        var now = DateTime.UtcNow;
        var role = new Role
        {
            Name = name,
            DisplayName = request.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsManager = request.IsManager,
            DepartmentId = request.DepartmentId,
            IsActive = true,
            CreatedAt = now
        };
        _context.Roles.Add(role);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.ROLE_CREATED,
            $"Rol oluşturuldu: {role.Name}.");

        await AttachDepartmentAsync(role);
        return MapRole(role);
    }

    public async Task<RoleListDto> UpdateAsync(int actorUserId, int id, UpdateRoleRequestDto request)
    {
        var role = await GetRequiredRoleAsync(id);
        var name = NormalizeRoleName(request.Name);
        await EnsureRoleNameAvailableAsync(name, excludeRoleId: id);
        await EnsureDepartmentExistsAsync(request.DepartmentId);

        role.Name = name;
        role.DisplayName = request.DisplayName.Trim();
        role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        role.IsManager = request.IsManager;
        role.DepartmentId = request.DepartmentId;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.ROLE_UPDATED,
            $"Rol güncellendi: {role.Name}.");

        await AttachDepartmentAsync(role);
        return MapRole(role);
    }

    public async Task<RoleListDto> SetActiveAsync(int actorUserId, int id, SetRoleActiveRequestDto request)
    {
        var role = await GetRequiredRoleAsync(id);
        if (role.IsActive == request.IsActive)
        {
            return MapRole(role);
        }

        role.IsActive = request.IsActive;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            request.IsActive ? AuthActivityType.ROLE_UPDATED : AuthActivityType.ROLE_DEACTIVATED,
            request.IsActive
                ? $"Rol yeniden aktif edildi: {role.Name}."
                : $"Rol pasife alındı: {role.Name}.");

        return MapRole(role);
    }

    public async Task DeleteAsync(int actorUserId, int id)
    {
        var role = await GetRequiredRoleAsync(id);
        var now = DateTime.UtcNow;
        role.IsActive = false;
        role.DeletedAt = now;

        var assignments = await _context.UserRoles
            .Where(ur => ur.RoleId == id && ur.IsActive)
            .ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.IsActive = false;
            assignment.DeletedAt = now;
        }

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.ROLE_DELETED,
            $"Rol silindi: {role.Name}.");
    }

    public async Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(int userId)
    {
        await EnsureUserExistsAsync(userId);

        var now = DateTime.UtcNow;
        var assignments = await _context.UserRoles
            .AsNoTracking()
            .Where(ur =>
                ur.UserId == userId &&
                ur.IsActive &&
                (ur.ExpiresAt == null || ur.ExpiresAt > now) &&
                ur.Role.IsActive)
            .OrderBy(ur => ur.Role.DisplayName)
            .Select(ur => new UserRoleDto
            {
                RoleId = ur.RoleId,
                Name = ur.Role.Name,
                DisplayName = ur.Role.DisplayName,
                IsManager = ur.Role.IsManager,
                AssignedAt = ur.AssignedAt,
                AssignedBy = ur.AssignedBy,
                ExpiresAt = ur.ExpiresAt
            })
            .ToListAsync();

        return assignments;
    }

    public async Task<UserRoleDto> AssignToUserAsync(int actorUserId, int userId, AssignRoleRequestDto request)
    {
        await EnsureUserExistsAsync(userId);

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role is null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }

        if (!role.IsActive)
        {
            throw new BadRequestException("Pasif bir rol atanamaz.");
        }

        if (request.ExpiresAt.HasValue && request.ExpiresAt.Value <= DateTime.UtcNow)
        {
            throw new BadRequestException("Rol bitiş tarihi gelecekte olmalıdır.");
        }

        var existing = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == request.RoleId);

        if (existing is not null && existing.IsActive && existing.DeletedAt is null)
        {
            throw new ConflictException("Bu rol kullanıcıya zaten atanmış.");
        }

        var now = DateTime.UtcNow;
        UserRole assignment;
        if (existing is not null)
        {
            existing.IsActive = true;
            existing.DeletedAt = null;
            existing.AssignedAt = now;
            existing.AssignedBy = actorUserId;
            existing.ExpiresAt = request.ExpiresAt;
            existing.CreatedAt ??= now;
            assignment = existing;
        }
        else
        {
            assignment = new UserRole
            {
                UserId = userId,
                RoleId = role.Id,
                AssignedAt = now,
                AssignedBy = actorUserId,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                CreatedAt = now
            };
            _context.UserRoles.Add(assignment);
        }

        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            userId,
            AuthActivityType.ROLE_ASSIGNED,
            $"Rol atandı: {role.Name} (işlemi yapan: {actorUserId}).");

        return new UserRoleDto
        {
            RoleId = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            IsManager = role.IsManager,
            AssignedAt = assignment.AssignedAt,
            AssignedBy = assignment.AssignedBy,
            ExpiresAt = assignment.ExpiresAt
        };
    }

    public async Task RevokeFromUserAsync(int actorUserId, int userId, int roleId)
    {
        await EnsureUserExistsAsync(userId);

        var assignment = await _context.UserRoles
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.IsActive);

        if (assignment is null)
        {
            throw new NotFoundException("Kullanıcıda bu rol bulunamadı.");
        }

        var now = DateTime.UtcNow;
        assignment.IsActive = false;
        assignment.DeletedAt = now;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            userId,
            AuthActivityType.ROLE_REVOKED,
            $"Rol alındı: {assignment.Role.Name} (işlemi yapan: {actorUserId}).");
    }

    private async Task<Role> GetRequiredRoleAsync(int id)
    {
        var role = await _context.Roles
            .Include(r => r.Department)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }

        return role;
    }

    private async Task EnsureRoleNameAvailableAsync(string name, int? excludeRoleId = null)
    {
        var exists = await _context.Roles.AnyAsync(r =>
            r.Name == name && (!excludeRoleId.HasValue || r.Id != excludeRoleId.Value));
        if (exists)
        {
            throw new ConflictException("Bu rol adı zaten kullanılıyor.");
        }
    }

    private async Task EnsureDepartmentExistsAsync(int? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return;
        }

        var exists = await _context.Departments
            .AsNoTracking()
            .AnyAsync(d => d.Id == departmentId.Value && d.IsActive);
        if (!exists)
        {
            throw new BadRequestException("Geçerli bir departman seçilmedi.");
        }
    }

    private static string NormalizeRoleName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("Rol adı boş olamaz.");
        }

        return trimmed;
    }

    private async Task EnsureUserExistsAsync(int userId)
    {
        var exists = await _context.Users.AsNoTracking().AnyAsync(u => u.Id == userId);
        if (!exists)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }
    }

    private async Task AttachDepartmentAsync(Role role)
    {
        if (!role.DepartmentId.HasValue)
        {
            role.Department = null;
            return;
        }

        if (role.Department is not null && role.Department.Id == role.DepartmentId.Value)
        {
            return;
        }

        role.Department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == role.DepartmentId.Value);
    }

    private static RoleListDto MapRole(Role role)
    {
        return new RoleListDto
        {
            Id = role.Id,
            Name = role.Name,
            DisplayName = role.DisplayName,
            Description = role.Description,
            IsActive = role.IsActive,
            IsManager = role.IsManager,
            DepartmentId = role.DepartmentId,
            DepartmentName = role.Department?.Name
        };
    }
}
