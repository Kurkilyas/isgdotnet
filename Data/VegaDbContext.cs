using InvoiceTrackingSystemBackend.Entities.Vega;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Data;

/// <summary>VEGA ERP veritabanına salt-okunur erişim için DbContext. Bu context için migration üretilmez.</summary>
public class VegaDbContext : DbContext
{
    public VegaDbContext(DbContextOptions<VegaDbContext> options) : base(options)
    {
    }

    public DbSet<TblCari> TblCaris { get; set; }
    public DbSet<TblMuhCariHesapKodlari> TblMuhCariHesapKodlaris { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TblCari>(entity =>
        {
            entity.ToTable("F0102TBLCARI");
            entity.HasKey(e => e.Ind);

            entity.Property(e => e.Resim).HasColumnName("RESIM");
            entity.Property(e => e.Ind).HasColumnName("IND");
            entity.Property(e => e.FirmaNo).HasColumnName("FIRMANO");
            entity.Property(e => e.EFaturaSenaryo).HasColumnName("EFATURASENARYO");
            entity.Property(e => e.KrediLimitiKontrol).HasColumnName("KREDILIMITIKONTROL");
            entity.Property(e => e.OdemeSekli).HasColumnName("ODEMESEKLI");
            entity.Property(e => e.DepoInd).HasColumnName("DEPOIND");
            entity.Property(e => e.TaksitTipi).HasColumnName("TAKSITTIPI");
            entity.Property(e => e.TaksitGunu).HasColumnName("TAKSITGUNU");
            entity.Property(e => e.SatisKarti).HasColumnName("SATISKARTI");
            entity.Property(e => e.FirmaTipi).HasColumnName("FIRMATIPI");
            entity.Property(e => e.Statu).HasColumnName("STATU");
            entity.Property(e => e.Opsiyon).HasColumnName("OPSIYON");
            entity.Property(e => e.ZimFiyat).HasColumnName("ZIMFIYAT");
            entity.Property(e => e.Status).HasColumnName("STATUS");
            entity.Property(e => e.PersonelNo).HasColumnName("PERSONELNO");
            entity.Property(e => e.Depo).HasColumnName("DEPO");
            entity.Property(e => e.Sektor).HasColumnName("SEKTOR");
            entity.Property(e => e.Marka).HasColumnName("MARKA");
            entity.Property(e => e.KayitTarihi).HasColumnName("KAYITTARIHI");
            entity.Property(e => e.Istihbarat).HasColumnName("ISTIHBARAT");
            entity.Property(e => e.KefilAdres2).HasColumnName("KEFILADRES2");
            entity.Property(e => e.KefilAdres1).HasColumnName("KEFILADRES1");
            entity.Property(e => e.AdresPosta).HasColumnName("ADRESPOSTA");
            entity.Property(e => e.AdresFatura).HasColumnName("ADRESFATURA");
            entity.Property(e => e.AdresSevk).HasColumnName("ADRESSEVK");
            entity.Property(e => e.AskSatisKarti).HasColumnName("ASKSATISKARTI");
            entity.Property(e => e.Deleted).HasColumnName("DELETED");
            entity.Property(e => e.EFaturaKullanicisi).HasColumnName("EFATURAKULLANICISI");
            entity.Property(e => e.IadeFaturasiKesilmesin).HasColumnName("IADEFATURASIKESILMESIN");
            entity.Property(e => e.TahsilatYapilmasin).HasColumnName("TAHSILATYAPILMASIN");
            entity.Property(e => e.BagKurCalismasin).HasColumnName("BAGKURCALISMASIN");
            entity.Property(e => e.SatisYapilmasin).HasColumnName("SATISYAPILMASIN");
            entity.Property(e => e.RiskLimiti).HasColumnName("RISKLIMITI").HasColumnType("decimal(28,8)");
            entity.Property(e => e.KrediLimiti).HasColumnName("KREDILIMITI").HasColumnType("decimal(28,8)");
            entity.Property(e => e.Iskonto).HasColumnName("ISKONTO").HasColumnType("decimal(28,8)");
            entity.Property(e => e.AylikVade).HasColumnName("AYLIKVADE").HasColumnType("decimal(28,8)");
            entity.Property(e => e.Prim).HasColumnName("PRIM").HasColumnType("decimal(28,8)");
            entity.Property(e => e.GecikmeFaizi).HasColumnName("GECIKMEFAIZI").HasColumnType("decimal(28,8)");
            entity.Property(e => e.OdemeBakiyesi).HasColumnName("ODEMEBAKIYESI").HasColumnType("decimal(28,8)");
            entity.Property(e => e.Bakiye).HasColumnName("BAKIYE").HasColumnType("decimal(28,8)");
            entity.Property(e => e.Unvan).HasColumnName("UNVAN");
            entity.Property(e => e.Kod3).HasColumnName("KOD3");
            entity.Property(e => e.Kod4).HasColumnName("KOD4");
            entity.Property(e => e.Kod5).HasColumnName("KOD5");
            entity.Property(e => e.Kod1).HasColumnName("KOD1");
            entity.Property(e => e.Kod2).HasColumnName("KOD2");
            entity.Property(e => e.Email).HasColumnName("EMAIL");
            entity.Property(e => e.Url).HasColumnName("URL");
            entity.Property(e => e.Telefon1).HasColumnName("TELEFON1");
            entity.Property(e => e.Telefon2).HasColumnName("TELEFON2");
            entity.Property(e => e.Soyadi).HasColumnName("SOYADI");
            entity.Property(e => e.Adi).HasColumnName("ADI");
            entity.Property(e => e.YDahili).HasColumnName("YDAHILI");
            entity.Property(e => e.YTelefon1).HasColumnName("YTELEFON1");
            entity.Property(e => e.YTelefon2).HasColumnName("YTELEFON2");
            entity.Property(e => e.Telefon3).HasColumnName("TELEFON3");
            entity.Property(e => e.Faks).HasColumnName("FAKS");
            entity.Property(e => e.Modem).HasColumnName("MODEM");
            entity.Property(e => e.YModem).HasColumnName("YMODEM");
            entity.Property(e => e.Kefil1).HasColumnName("KEFIL1");
            entity.Property(e => e.Kefil2).HasColumnName("KEFIL2");
            entity.Property(e => e.Pozisyon).HasColumnName("POZISYON");
            entity.Property(e => e.YEmail).HasColumnName("YEMAIL");
            entity.Property(e => e.YUrl).HasColumnName("YURL");
            entity.Property(e => e.Kefil1Telefon).HasColumnName("KEFIL1TELEFON");
            entity.Property(e => e.Kefil1CepTelefon).HasColumnName("KEFIL1CEPTELEFON");
            entity.Property(e => e.Kefil2Telefon).HasColumnName("KEFIL2TELEFON");
            entity.Property(e => e.Kefil2CepTelefon).HasColumnName("KEFIL2CEPTELEFON");
            entity.Property(e => e.YGsm).HasColumnName("YGSM");
            entity.Property(e => e.YFaks).HasColumnName("YFAKS");
            entity.Property(e => e.Kefil1TakipKodu).HasColumnName("KEFIL1TAKIPKODU");
            entity.Property(e => e.Kefil2TakipKodu).HasColumnName("KEFIL2TAKIPKODU");
            entity.Property(e => e.Kefil1TcKimlikNo).HasColumnName("KEFIL1TCKIMLIKNO");
            entity.Property(e => e.Kefil2TcKimlikNo).HasColumnName("KEFIL2TCKIMLIKNO");
            entity.Property(e => e.FirmaTakipKodu).HasColumnName("FIRMATAKIPKODU");
            entity.Property(e => e.ParaBirimi).HasColumnName("PARABIRIMI");
            entity.Property(e => e.Yetkili).HasColumnName("YETKILI");
            entity.Property(e => e.VergiDairesi).HasColumnName("VERGIDAIRESI");
            entity.Property(e => e.VergiNo).HasColumnName("VERGINO");
            entity.Property(e => e.Il).HasColumnName("IL");
            entity.Property(e => e.Sehir).HasColumnName("SEHIR");
            entity.Property(e => e.Alias).HasColumnName("ALIAS");
            entity.Property(e => e.FirmaKodu).HasColumnName("FIRMAKODU");
            entity.Property(e => e.FirmaAdi).HasColumnName("FIRMAADI");
            entity.Property(e => e.Sermaye).HasColumnName("SERMAYE").HasColumnType("decimal(28,8)");
            entity.Property(e => e.SicilNo).HasColumnName("SICILNO");
            entity.Property(e => e.ETicaret).HasColumnName("ETICARET");
            entity.Property(e => e.IseGirisTarihi).HasColumnName("ISEGIRISTARIHI");
            entity.Property(e => e.IstenCikisTarihi).HasColumnName("ISTENCIKISTARIHI");
            entity.Property(e => e.OdemeYapilmasin).HasColumnName("ODEMEYAPILMASIN");
            entity.Property(e => e.GrupKodu).HasColumnName("GRUPKODU");
            entity.Property(e => e.NaceKodu).HasColumnName("NACEKODU");
            entity.Property(e => e.HedefCiro).HasColumnName("HEDEFCIRO").HasColumnType("decimal(28,8)");
            entity.Property(e => e.SmsGonder).HasColumnName("SMSGONDER");
            entity.Property(e => e.EmailGonder).HasColumnName("EMAILGONDER");
            entity.Property(e => e.EArsivTeslimTipi).HasColumnName("EARSIVTESLIMTIPI");
            entity.Property(e => e.Kod6).HasColumnName("KOD6");
            entity.Property(e => e.Kod7).HasColumnName("KOD7");
            entity.Property(e => e.IsletmeTuru).HasColumnName("ISLETMETURU");
            entity.Property(e => e.GlnKodu).HasColumnName("GLNKODU");
            entity.Property(e => e.SubeAdi).HasColumnName("SUBEADI");
            entity.Property(e => e.Uid).HasColumnName("UID");
            entity.Property(e => e.KdvMuafiyati).HasColumnName("KDVMUAFIYATI");
            entity.Property(e => e.CariPozisyon).HasColumnName("CARIPOZISYON");
            entity.Property(e => e.AlisYapilmasin).HasColumnName("ALISYAPILMASIN");
            entity.Property(e => e.SiparisYapilmasin).HasColumnName("SIPARISYAPILMASIN");
            entity.Property(e => e.GuncellemeTarihi).HasColumnName("GUNCELLEMETARIHI");
            entity.Property(e => e.KurTipi).HasColumnName("KURTIPI");
            entity.Property(e => e.EIrsaliye).HasColumnName("EIRSALIYE");
            entity.Property(e => e.EIrsaliyeAlias).HasColumnName("EIRSALIYEALIAS");
            entity.Property(e => e.UtsKurumNo).HasColumnName("UTSKURUMNO");
            entity.Property(e => e.AliasGuncellenmesin).HasColumnName("ALIASGUNCELLENMESIN");
            entity.Property(e => e.IstihbaratBelgedeGorunsun).HasColumnName("ISTIHBARATBELGEDEGORUNSUN");
            entity.Property(e => e.SmEmail).HasColumnName("SMEMAIL");
            entity.Property(e => e.PostaKodu).HasColumnName("POSTAKODU");
            entity.Property(e => e.SmsIzniVar).HasColumnName("SMSIZNIVAR");
            entity.Property(e => e.KvkkIzniVar).HasColumnName("KVKKIZNIVAR");
            entity.Property(e => e.StcNo).HasColumnName("STCNO");
            entity.Property(e => e.Sadi).HasColumnName("SADI");
            entity.Property(e => e.SSoyadi).HasColumnName("SSOYADI");
            entity.Property(e => e.Plaka).HasColumnName("PLAKA");
        });

        modelBuilder.Entity<TblMuhCariHesapKodlari>(entity =>
        {
            entity.ToTable("F0102D0013TBLMUHCARIHESAPKODLARI");
            entity.HasKey(e => e.Ind);

            entity.Property(e => e.Ind).HasColumnName("IND");
            entity.Property(e => e.FirmaNo).HasColumnName("FIRMANO");
            entity.Property(e => e.BorcKisaVade).HasColumnName("BORCKISAVADE");
            entity.Property(e => e.AlacakKisaVade).HasColumnName("ALACAKKISAVADE");
            entity.Property(e => e.BorcUzunVade).HasColumnName("BORCUZUNVADE");
            entity.Property(e => e.AlacakUzunVade).HasColumnName("ALACAKUZUNVADE");
            entity.Property(e => e.GiderCesitKodu).HasColumnName("GIDERCESITKODU");
        });
    }
}
