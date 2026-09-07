using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Vega;

namespace InvoiceTrackingSystemBackend.Interfaces.Vega;

public interface ITblCariService
{
    Task<PagedResult<TblCariListDto>> GetListAsync(int page = 1, int pageSize = 20);
    Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null);
    Task<TblCariDetailDto?> GetByIdAsync(int id);
}
