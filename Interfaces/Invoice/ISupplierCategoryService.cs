using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface ISupplierCategoryService
{
    Task<PagedResult<SupplierCategoryResponseDto>> GetListAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync();
    Task<SupplierCategoryResponseDto> GetByIdAsync(int id);
    Task<SupplierCategoryResponseDto> CreateAsync(CreateSupplierCategoryRequestDto request);
    Task<SupplierCategoryResponseDto> UpdateAsync(int id, UpdateSupplierCategoryRequestDto request);
    Task<SupplierCategoryResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
}
