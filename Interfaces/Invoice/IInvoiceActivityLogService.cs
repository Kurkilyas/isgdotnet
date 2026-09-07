using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceActivityLogService
{
    Task LogAsync(int invoiceId, InvoiceActivityType activityType, int? userId, int? workflowStepId = null, string? description = null);
    Task<PagedResult<InvoiceActivityLogResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? userId = null,
        InvoiceActivityType? activityType = null,
        string? description = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync();
    Task<InvoiceActivityLogResponseDto?> GetByIdAsync(int id);
}
