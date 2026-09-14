using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.Data;
using InvoiceTrackingSystemBackend.DTOs.Auth;
using InvoiceTrackingSystemBackend.DTOs.Vega;
using InvoiceTrackingSystemBackend.Exceptions;
using InvoiceTrackingSystemBackend.Interfaces.Vega;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Services.Vega;

public class TblCariService : ITblCariService
{
    private readonly VegaDbContext _context;

    public TblCariService(VegaDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<TblCariListDto>> GetListAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        try
        {
            var query = _context.TblCaris.AsNoTracking();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(e => e.Ind)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new TblCariListDto
                {
                    Ind = e.Ind,
                    FirmaKodu = e.FirmaKodu,
                    FirmaAdi = e.FirmaAdi,
                    AdresFatura = e.AdresFatura
                })
                .ToListAsync();

            return PagedResult<TblCariListDto>.Create(items, totalCount, page, pageSize);
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }

    public async Task<IReadOnlyList<IdNameDto>> GetAllAsync(bool? isActive = null, string? name = null)
    {
        try
        {
            var query = _context.TblCaris.AsNoTracking();
            if (isActive.HasValue)
            {
                query = isActive.Value
                    ? query.Where(e => e.Deleted != true)
                    : query.Where(e => e.Deleted == true);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var term = name.Trim();
                query = query.Where(e =>
                    (e.FirmaAdi != null && e.FirmaAdi.Contains(term)) ||
                    (e.Unvan != null && e.Unvan.Contains(term)) ||
                    (e.FirmaKodu != null && e.FirmaKodu.Contains(term)));
            }

            return await query
                .OrderBy(e => e.FirmaAdi)
                .ThenBy(e => e.Ind)
                .Select(e => new IdNameDto
                {
                    Id = e.Ind,
                    Name = e.FirmaAdi ?? e.Unvan ?? e.FirmaKodu ?? e.Ind.ToString()
                })
                .ToListAsync();
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }

    public async Task<TblCariDetailDto?> GetByIdAsync(int id)
    {
        try
        {
            return await GetByIdInternalAsync(id);
        }
        catch (SqlException ex)
        {
            throw new ExternalServiceException("Vega (ERP) veritabanına şu anda ulaşılamıyor.", ex);
        }
    }

    private async Task<TblCariDetailDto?> GetByIdInternalAsync(int id)
    {
        return await _context.TblCaris
            .AsNoTracking()
            .Where(e => e.Ind == id)
            .Select(e => new TblCariDetailDto
            {
                Ind = e.Ind,
                FirmaNo = e.FirmaNo,
                EFaturaSenaryo = e.EFaturaSenaryo,
                KrediLimitiKontrol = e.KrediLimitiKontrol,
                OdemeSekli = e.OdemeSekli,
                DepoInd = e.DepoInd,
                TaksitTipi = e.TaksitTipi,
                TaksitGunu = e.TaksitGunu,
                SatisKarti = e.SatisKarti,
                FirmaTipi = e.FirmaTipi,
                Statu = e.Statu,
                Opsiyon = e.Opsiyon,
                ZimFiyat = e.ZimFiyat,
                Status = e.Status,
                PersonelNo = e.PersonelNo,
                Depo = e.Depo,
                Sektor = e.Sektor,
                Marka = e.Marka,
                KayitTarihi = e.KayitTarihi,
                Istihbarat = e.Istihbarat,
                KefilAdres2 = e.KefilAdres2,
                KefilAdres1 = e.KefilAdres1,
                AdresPosta = e.AdresPosta,
                AdresFatura = e.AdresFatura,
                AdresSevk = e.AdresSevk,
                AskSatisKarti = e.AskSatisKarti,
                Deleted = e.Deleted,
                EFaturaKullanicisi = e.EFaturaKullanicisi,
                IadeFaturasiKesilmesin = e.IadeFaturasiKesilmesin,
                TahsilatYapilmasin = e.TahsilatYapilmasin,
                BagKurCalismasin = e.BagKurCalismasin,
                SatisYapilmasin = e.SatisYapilmasin,
                RiskLimiti = e.RiskLimiti,
                KrediLimiti = e.KrediLimiti,
                Iskonto = e.Iskonto,
                AylikVade = e.AylikVade,
                Prim = e.Prim,
                GecikmeFaizi = e.GecikmeFaizi,
                OdemeBakiyesi = e.OdemeBakiyesi,
                Bakiye = e.Bakiye,
                Unvan = e.Unvan,
                Kod3 = e.Kod3,
                Kod4 = e.Kod4,
                Kod5 = e.Kod5,
                Kod1 = e.Kod1,
                Kod2 = e.Kod2,
                Email = e.Email,
                Url = e.Url,
                Telefon1 = e.Telefon1,
                Telefon2 = e.Telefon2,
                Soyadi = e.Soyadi,
                Adi = e.Adi,
                YDahili = e.YDahili,
                YTelefon1 = e.YTelefon1,
                YTelefon2 = e.YTelefon2,
                Telefon3 = e.Telefon3,
                Faks = e.Faks,
                Modem = e.Modem,
                YModem = e.YModem,
                Kefil1 = e.Kefil1,
                Kefil2 = e.Kefil2,
                Pozisyon = e.Pozisyon,
                YEmail = e.YEmail,
                YUrl = e.YUrl,
                Kefil1Telefon = e.Kefil1Telefon,
                Kefil1CepTelefon = e.Kefil1CepTelefon,
                Kefil2Telefon = e.Kefil2Telefon,
                Kefil2CepTelefon = e.Kefil2CepTelefon,
                YGsm = e.YGsm,
                YFaks = e.YFaks,
                Kefil1TakipKodu = e.Kefil1TakipKodu,
                Kefil2TakipKodu = e.Kefil2TakipKodu,
                Kefil1TcKimlikNo = e.Kefil1TcKimlikNo,
                Kefil2TcKimlikNo = e.Kefil2TcKimlikNo,
                FirmaTakipKodu = e.FirmaTakipKodu,
                ParaBirimi = e.ParaBirimi,
                Yetkili = e.Yetkili,
                VergiDairesi = e.VergiDairesi,
                VergiNo = e.VergiNo,
                Il = e.Il,
                Sehir = e.Sehir,
                Alias = e.Alias,
                FirmaKodu = e.FirmaKodu,
                FirmaAdi = e.FirmaAdi,
                Sermaye = e.Sermaye,
                SicilNo = e.SicilNo,
                ETicaret = e.ETicaret,
                IseGirisTarihi = e.IseGirisTarihi,
                IstenCikisTarihi = e.IstenCikisTarihi,
                OdemeYapilmasin = e.OdemeYapilmasin,
                GrupKodu = e.GrupKodu,
                NaceKodu = e.NaceKodu,
                HedefCiro = e.HedefCiro,
                SmsGonder = e.SmsGonder,
                EmailGonder = e.EmailGonder,
                EArsivTeslimTipi = e.EArsivTeslimTipi,
                Kod6 = e.Kod6,
                Kod7 = e.Kod7,
                IsletmeTuru = e.IsletmeTuru,
                GlnKodu = e.GlnKodu,
                SubeAdi = e.SubeAdi,
                Uid = e.Uid,
                KdvMuafiyati = e.KdvMuafiyati,
                CariPozisyon = e.CariPozisyon,
                AlisYapilmasin = e.AlisYapilmasin,
                SiparisYapilmasin = e.SiparisYapilmasin,
                GuncellemeTarihi = e.GuncellemeTarihi,
                KurTipi = e.KurTipi,
                EIrsaliye = e.EIrsaliye,
                EIrsaliyeAlias = e.EIrsaliyeAlias,
                UtsKurumNo = e.UtsKurumNo,
                AliasGuncellenmesin = e.AliasGuncellenmesin,
                IstihbaratBelgedeGorunsun = e.IstihbaratBelgedeGorunsun,
                SmEmail = e.SmEmail,
                PostaKodu = e.PostaKodu,
                SmsIzniVar = e.SmsIzniVar,
                KvkkIzniVar = e.KvkkIzniVar,
                StcNo = e.StcNo,
                Sadi = e.Sadi,
                SSoyadi = e.SSoyadi,
                Plaka = e.Plaka
            })
            .FirstOrDefaultAsync();
    }
}
