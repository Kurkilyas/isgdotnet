using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceNotificationLogService
{
    Task LogAsync(
        int invoiceId,
        string recipientEmail,
        NotificationType notificationType,
        int? recipientUserId = null,
        int? workflowStepId = null,
        bool isSuccess = true,
        string? errorMessage = null);
    Task<PagedResult<InvoiceNotificationLogResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? recipientUserId = null,
        NotificationType? notificationType = null,
        bool? isSuccess = null);
    Task<InvoiceNotificationLogResponseDto?> GetByIdAsync(int id);
}
