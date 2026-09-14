using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IRoleService
{
    Task<PagedResult<RoleListDto>> GetListAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null);
    Task<RoleListDto> GetByIdAsync(int id);
    Task<RoleListDto> CreateAsync(int actorUserId, CreateRoleRequestDto request);
    Task<RoleListDto> UpdateAsync(int actorUserId, int id, UpdateRoleRequestDto request);
    Task<RoleListDto> SetActiveAsync(int actorUserId, int id, SetRoleActiveRequestDto request);
    Task DeleteAsync(int actorUserId, int id);
    Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(int userId);
    Task<UserRoleDto> AssignToUserAsync(int actorUserId, int userId, AssignRoleRequestDto request);
    Task RevokeFromUserAsync(int actorUserId, int userId, int roleId);
}
