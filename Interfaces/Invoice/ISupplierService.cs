using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Invoice;

namespace InvoiceTrackingSystemBackend.Interfaces.Invoice;

public interface ISupplierService
{
    Task<PagedResult<SupplierListDto>> GetListAsync(int page = 1, int pageSize = 20, string? search = null, bool? isActive = null, int? supplierCategoryId = null);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync();
    Task<SupplierResponseDto> GetByIdAsync(int id);
    Task<SupplierResponseDto> CreateAsync(CreateSupplierRequestDto request);
    Task<SupplierResponseDto> UpdateAsync(int id, UpdateSupplierRequestDto request);
    Task<SupplierResponseDto> SetActiveAsync(int id, SetActiveRequestDto request);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<SupplierInvoiceTypeResponseDto>> GetInvoiceTypesAsync(int supplierId);
    Task<IReadOnlyList<SupplierInvoiceTypeResponseDto>> AssignInvoiceTypesAsync(int supplierId, AssignSupplierInvoiceTypesRequestDto request);
    Task RevokeInvoiceTypeAsync(int supplierId, int invoiceTypeId);
}
