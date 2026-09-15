using isgDotnet.Common;
using isgDotnet.Constants;
using isgDotnet.DTOs.Auth;

namespace isgDotnet.Interfaces.Auth;

public interface IUserActivityLogService
{
    Task LogAsync(int? userId, AuthActivityType activityType, string? description = null);
    Task<PagedResult<UserActivityLogDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? userId = null,
        string? description = null,
        AuthActivityType? activityType = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(string? name = null);
    Task<UserActivityLogDto?> GetByIdAsync(int id);
}
