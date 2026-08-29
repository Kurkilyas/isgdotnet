using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceWorkflowHistoryService
{
    Task LogAsync(
        int invoiceId,
        WorkflowActionType actionType,
        InvoiceStatus toStatus,
        InvoiceStatus? fromStatus = null,
        int? actorUserId = null,
        string? reason = null);
    Task<PagedResult<InvoiceWorkflowHistoryResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? actorUserId = null,
        WorkflowActionType? actionType = null);
    Task<InvoiceWorkflowHistoryResponseDto?> GetByIdAsync(int id);
}
