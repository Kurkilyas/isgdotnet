using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface IInvoiceTypeService
{
    Task<PagedResult<InvoiceTypeListDto>> GetListAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync();
    Task<InvoiceTypeResponseDto> GetByIdAsync(int id);
    Task<InvoiceTypeResponseDto> CreateAsync(CreateInvoiceTypeRequestDto request);
    Task<InvoiceTypeResponseDto> UpdateAsync(int id, UpdateInvoiceTypeRequestDto request);
    Task<InvoiceTypeResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
}
