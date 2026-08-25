using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IRoleService
{
    Task<IReadOnlyList<RoleListDto>> GetListAsync();
    Task<RoleListDto> GetByIdAsync(int id);
    Task<RoleListDto> CreateAsync(int actorUserId, CreateRoleRequestDto request);
    Task<RoleListDto> UpdateAsync(int actorUserId, int id, UpdateRoleRequestDto request);
    Task<RoleListDto> SetActiveAsync(int actorUserId, int id, SetRoleActiveRequestDto request);
    Task DeleteAsync(int actorUserId, int id);
    Task<IReadOnlyList<UserRoleDto>> GetUserRolesAsync(int userId);
    Task<UserRoleDto> AssignToUserAsync(int actorUserId, int userId, AssignRoleRequestDto request);
    Task RevokeFromUserAsync(int actorUserId, int userId, int roleId);
}
