using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceAttachmentService
{
    Task<PagedResult<InvoiceAttachmentResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        int? workflowStepId = null,
        int? uploadedByUserId = null,
        string? search = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(int? invoiceId = null, string? name = null);
    Task<InvoiceAttachmentResponseDto> GetByIdAsync(int id);
    Task<IReadOnlyList<InvoiceAttachmentResponseDto>> CreateAsync(
        int invoiceId,
        int actorUserId,
        IReadOnlyList<IFormFile> files);
    Task<InvoiceAttachmentResponseDto> UpdateAsync(
        int id,
        int actorUserId,
        string? fileName,
        IFormFile? file);
    Task DeleteAsync(int id, int actorUserId);
    Task<(Stream Content, string ContentType, string FileName)> OpenContentAsync(int id, int actorUserId);
}
