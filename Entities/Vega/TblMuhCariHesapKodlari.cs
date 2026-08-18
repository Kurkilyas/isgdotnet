namespace InvoiceTrackingSystemBackend.Entities.Vega;

/// <summary>VEGA ERP - F0102D0013TBLMUHCARIHESAPKODLARI tablosuna karşılık gelen salt-okunur entity.</summary>
public class TblMuhCariHesapKodlari
{
    public int Ind { get; set; }
    public int? FirmaNo { get; set; }
    public string? BorcKisaVade { get; set; }
    public string? AlacakKisaVade { get; set; }
    public string? BorcUzunVade { get; set; }
    public string? AlacakUzunVade { get; set; }
    public string? GiderCesitKodu { get; set; }
}
