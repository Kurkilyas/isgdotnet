using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Bir carinin kesebileceği fatura türü ADAYLARI (çoktan-çoğa). Tek aday varsa otomatik, birden fazla varsa ML/manuel ayrım yapılır.</summary>
public class SupplierInvoiceType : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int InvoiceTypeId { get; set; }
    public InvoiceType InvoiceType { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
