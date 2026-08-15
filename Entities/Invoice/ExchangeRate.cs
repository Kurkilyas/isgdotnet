using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Her gün 1 kez background job ile doldurulur, fatura anında canlı API çağrısı yapılmaz.</summary>
public class ExchangeRate
{
    public int Id { get; set; }

    [MaxLength(10)]
    public string CurrencyCode { get; set; } = null!;

    public DateOnly RateDate { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal RateToTry { get; set; }

    /// <summary>TCMB / MANUAL</summary>
    [MaxLength(30)]
    public string Source { get; set; } = "TCMB";

    public DateTime CreatedAt { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
