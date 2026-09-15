using isgDotnet.Common;
using isgDotnet.DTOs.Auth;

namespace isgDotnet.Interfaces.Auth;

public interface IPermissionService
{
    Task<PagedResult<PermissionListDto>> GetListAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null);
    Task<PermissionListDto> GetByIdAsync(int id);
    Task<PermissionListDto> CreateAsync(int actorUserId, CreatePermissionRequestDto request);
    Task<PermissionListDto> UpdateAsync(int actorUserId, int id, UpdatePermissionRequestDto request);
    Task<PermissionListDto> SetActiveAsync(int actorUserId, int id, SetPermissionActiveRequestDto request);
    Task DeleteAsync(int actorUserId, int id);
    Task<IReadOnlyList<RolePermissionDto>> GetRolePermissionsAsync(int roleId);
    Task<IReadOnlyList<RolePermissionDto>> AssignToRoleAsync(int actorUserId, int roleId, AssignPermissionRequestDto request);
    Task RevokeFromRoleAsync(int actorUserId, int roleId, int permissionId);
}
