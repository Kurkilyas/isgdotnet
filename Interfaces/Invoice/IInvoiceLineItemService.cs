using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceLineItemService
{
    Task<PagedResult<InvoiceLineItemResponseDto>> GetListAsync(
        int page = 1,
        int pageSize = 20,
        int? invoiceId = null,
        string? search = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(int? invoiceId = null, string? name = null);
    Task<InvoiceLineItemResponseDto> GetByIdAsync(int id);
    Task<InvoiceLineItemResponseDto> CreateAsync(CreateInvoiceLineItemRequestDto request);
    Task<InvoiceLineItemResponseDto> UpdateAsync(int id, UpdateInvoiceLineItemRequestDto request);
    Task DeleteAsync(int id);
}
