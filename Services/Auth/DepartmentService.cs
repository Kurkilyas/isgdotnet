using isgDotnet.Common;
using isgDotnet.Constants;
using isgDotnet.Data;
using isgDotnet.DTOs.Auth;
using isgDotnet.Entities.Auth;
using isgDotnet.Exceptions;
using isgDotnet.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace isgDotnet.Services.Auth;

public class DepartmentService : IDepartmentService
{
    private readonly UserDbContext _context;
    private readonly IUserActivityLogService _activityLogService;

    public DepartmentService(UserDbContext context, IUserActivityLogService activityLogService)
    {
        _context = context;
        _activityLogService = activityLogService;
    }

    public async Task<PagedResult<DepartmentListDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(d =>
                d.Name.Contains(term) ||
                (d.Description != null && d.Description.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();
        var departments = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = departments.Select(MapDepartment).ToList();
        return PagedResult<DepartmentListDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        var query = _context.Departments.AsNoTracking();
        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim();
            query = query.Where(d => d.Name.Contains(term));
        }

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new IdNameDto
            {
                Id = d.Id,
                Name = d.Name
            })
            .ToListAsync();
    }

    public async Task<DepartmentListDto> GetByIdAsync(int id)
    {
        var department = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (department is null)
        {
            throw new NotFoundException("Departman bulunamadı.");
        }

        return MapDepartment(department);
    }

    public async Task<DepartmentListDto> CreateAsync(int actorUserId, CreateDepartmentRequestDto request)
    {
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name);

        var now = DateTime.UtcNow;
        var department = new Department
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsActive = true,
            CreatedAt = now
        };
        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.DEPARTMENT_CREATED,
            $"Departman oluşturuldu: {department.Name}.");

        return MapDepartment(department);
    }

    public async Task<DepartmentListDto> UpdateAsync(int actorUserId, int id, UpdateDepartmentRequestDto request)
    {
        var department = await GetRequiredAsync(id);
        var name = NormalizeName(request.Name);
        await EnsureNameAvailableAsync(name, excludeId: id);

        department.Name = name;
        department.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        department.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.DEPARTMENT_UPDATED,
            $"Departman güncellendi: {department.Name}.");

        return MapDepartment(department);
    }

    public async Task<DepartmentListDto> SetActiveAsync(int actorUserId, int id, SetDepartmentActiveRequestDto request)
    {
        var department = await GetRequiredAsync(id);
        if (department.IsActive == request.IsActive)
        {
            return MapDepartment(department);
        }

        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            request.IsActive ? AuthActivityType.DEPARTMENT_UPDATED : AuthActivityType.DEPARTMENT_DEACTIVATED,
            request.IsActive
                ? $"Departman yeniden aktif edildi: {department.Name}."
                : $"Departman pasife alındı: {department.Name}.");

        return MapDepartment(department);
    }

    public async Task DeleteAsync(int actorUserId, int id)
    {
        var department = await GetRequiredAsync(id);

        var hasRoles = await _context.Roles.AnyAsync(r => r.DepartmentId == id);
        if (hasRoles)
        {
            throw new ConflictException("Bu departmana bağlı roller var. Önce rollerin departmanını değiştirin.");
        }

        var now = DateTime.UtcNow;
        department.IsActive = false;
        department.DeletedAt = now;
        department.UpdatedAt = now;
        await _context.SaveChangesAsync();
        await _activityLogService.LogAsync(
            actorUserId,
            AuthActivityType.DEPARTMENT_DELETED,
            $"Departman silindi: {department.Name}.");
    }

    private async Task<Department> GetRequiredAsync(int id)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
        if (department is null)
        {
            throw new NotFoundException("Departman bulunamadı.");
        }

        return department;
    }

    private async Task EnsureNameAvailableAsync(string name, int? excludeId = null)
    {
        var exists = await _context.Departments.AnyAsync(d =>
            d.Name == name && (!excludeId.HasValue || d.Id != excludeId.Value));
        if (exists)
        {
            throw new ConflictException("Bu departman adı zaten kullanılıyor.");
        }
    }

    private static string NormalizeName(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new BadRequestException("Departman adı boş olamaz.");
        }

        return trimmed;
    }

    private static DepartmentListDto MapDepartment(Department department)
    {
        return new DepartmentListDto
        {
            Id = department.Id,
            Name = department.Name,
            Description = department.Description,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
    }
}
