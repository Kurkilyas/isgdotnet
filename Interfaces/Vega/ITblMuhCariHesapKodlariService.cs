using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Vega;

namespace InvoiceTrackingSystemBackend.Interfaces.Vega;

public interface ITblMuhCariHesapKodlariService
{
    Task<PagedResult<TblMuhCariHesapKodlariDto>> GetListAsync(int page = 1, int pageSize = 20);
    Task<TblMuhCariHesapKodlariDto?> GetByIdAsync(int id);
}
