using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Faturanın kalemleri (e-fatura/ERP'den gelir).</summary>
public class InvoiceLineItem : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>ML ve manuel sınıflandırma için ana veri kaynağı.</summary>
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Quantity { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? LineAmount { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
