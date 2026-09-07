using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Constants;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceListItemDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        InvoiceStatus? status = null,
        int? supplierId = null,
        int? invoiceTypeId = null,
        bool? isDuplicate = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync();
    Task<InvoiceDetailDto> GetByIdAsync(int id, int? viewerUserId = null);
    Task<InvoiceDetailDto> CreateAsync(CreateInvoiceRequestDto request, int? actorUserId = null);
    Task<InvoiceDetailDto> UpdateAsync(int id, UpdateInvoiceRequestDto request);
    Task DeleteAsync(int id);

    Task<IReadOnlyList<InvoiceLineItemResponseDto>> GetLineItemsAsync(int invoiceId);
    Task<InvoiceLineItemResponseDto> CreateLineItemAsync(int invoiceId, CreateInvoiceLineItemRequestDto request);
    Task<InvoiceLineItemResponseDto> UpdateLineItemAsync(int invoiceId, int lineItemId, UpdateInvoiceLineItemRequestDto request);
    Task DeleteLineItemAsync(int invoiceId, int lineItemId);

    Task<IReadOnlyList<InvoiceRelationResponseDto>> GetRelationsAsync(int invoiceId);
    Task<InvoiceRelationResponseDto> CreateRelationAsync(int invoiceId, CreateInvoiceRelationRequestDto request, int? createdByUserId);

    Task<IReadOnlyList<InvoiceWorkflowStepResponseDto>> GetWorkflowStepsAsync(int invoiceId);
    Task<IReadOnlyList<InvoiceAttachmentResponseDto>> GetAttachmentsAsync(int invoiceId);
}
