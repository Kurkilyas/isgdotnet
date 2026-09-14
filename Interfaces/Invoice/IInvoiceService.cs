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
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(string? name = null);
    Task<InvoiceDashboardSummaryDto> GetMySummaryAsync(int userId);
    Task<PagedResult<InvoiceListItemDto>> GetInboxAsync(int userId, int page = 1, int pageSize = 5);
    Task<InvoiceDetailDto> GetByIdAsync(int id, int? viewerUserId = null);
    Task<InvoiceDetailDto> CreateAsync(CreateInvoiceRequestDto request, int? actorUserId = null);
    Task<InvoiceDetailDto> UpdateAsync(int id, UpdateInvoiceRequestDto request, int? actorUserId = null);
    Task DeleteAsync(int id);
    Task<InvoiceDetailDto> ArchiveAsync(int id, int actorUserId);

    Task<IReadOnlyList<InvoiceRelationResponseDto>> GetRelationsAsync(int invoiceId);
    Task<InvoiceRelationResponseDto> CreateRelationAsync(int invoiceId, CreateInvoiceRelationRequestDto request, int? createdByUserId);

    Task<IReadOnlyList<InvoiceWorkflowStepResponseDto>> GetWorkflowStepsAsync(int invoiceId);
}
