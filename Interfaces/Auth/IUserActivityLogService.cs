using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

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
