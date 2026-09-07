using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Vega;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Vega;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Vega;

public class TblMuhCariHesapKodlariService : ITblMuhCariHesapKodlariService
{
    private readonly VegaDbContext _context;

    public TblMuhCariHesapKodlariService(VegaDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TblMuhCariHesapKodlariDto>> GetListAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        try
        {
            var query = _context.TblMuhCariHesapKodlaris.AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(e => e.Ind)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new TblMuhCariHesapKodlariDto
                {
                    Ind = e.Ind,
                    FirmaNo = e.FirmaNo,
                    BorcKisaVade = e.BorcKisaVade,
                    AlacakKisaVade = e.AlacakKisaVade,
                    BorcUzunVade = e.BorcUzunVade,
                    AlacakUzunVade = e.AlacakUzunVade,
                    GiderCesitKodu = e.GiderCesitKodu
                })
                .ToListAsync();

            return PagedResult<TblMuhCariHesapKodlariDto>.Create(items, totalCount, page, pageSize);
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync()
    {
        try
        {
            return await _context.TblMuhCariHesapKodlaris
                .AsNoTracking()
                .OrderBy(e => e.Ind)
                .Select(e => new IdNameDto
                {
                    Id = e.Ind,
                    Name = e.GiderCesitKodu ?? e.FirmaNo.ToString() ?? e.Ind.ToString()
                })
                .ToListAsync();
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }

    public async Task<TblMuhCariHesapKodlariDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.TblMuhCariHesapKodlaris
                .AsNoTracking()
                .Where(e => e.Ind == id)
                .Select(e => new TblMuhCariHesapKodlariDto
                {
                    Ind = e.Ind,
                    FirmaNo = e.FirmaNo,
                    BorcKisaVade = e.BorcKisaVade,
                    AlacakKisaVade = e.AlacakKisaVade,
                    BorcUzunVade = e.BorcUzunVade,
                    AlacakUzunVade = e.AlacakUzunVade,
                    GiderCesitKodu = e.GiderCesitKodu
                })
                .FirstOrDefaultAsync();
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }
}
