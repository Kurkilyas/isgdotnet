using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// İki fatura arasındaki mükerrer/fiyat farkı/alacak dekontu gibi ilişkileri tek noktadan yönetir.
/// Append-only, silinmez.
/// </summary>
public class InvoiceRelation
{
    public int Id { get; set; }

    /// <summary>İlişkiyi taşıyan/yeni fatura (ör. fiyat farkı faturası).</summary>
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>Bağlı olduğu asıl/orijinal fatura.</summary>
    public int RelatedInvoiceId { get; set; }
    public Invoice RelatedInvoice { get; set; } = null!;

    public InvoiceRelationType RelationType { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok. Otomatik ERP kontrolüyse null.</summary>
    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
