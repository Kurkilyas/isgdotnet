using isgDotnet.Common;
using isgDotnet.DTOs.Auth;

namespace isgDotnet.Interfaces.Auth;

public interface IDepartmentService
{
    Task<PagedResult<DepartmentListDto>> GetListAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null);
    Task<DepartmentListDto> GetByIdAsync(int id);
    Task<DepartmentListDto> CreateAsync(int actorUserId, CreateDepartmentRequestDto request);
    Task<DepartmentListDto> UpdateAsync(int actorUserId, int id, UpdateDepartmentRequestDto request);
    Task<DepartmentListDto> SetActiveAsync(int actorUserId, int id, SetDepartmentActiveRequestDto request);
    Task DeleteAsync(int actorUserId, int id);
}
