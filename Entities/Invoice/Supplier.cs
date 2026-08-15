using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Çalışılan firmalar (cariler). Departman ataması burada DEĞİL, invoice_type üzerinden yapılır. Fiziksel silme YASAK (soft delete).</summary>
public class Supplier : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Vergi Kimlik No / TC Kimlik No - firmanın benzersiz kimliği.</summary>
    [MaxLength(11)]
    public string VknTckn { get; set; } = null!;

    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public int? SupplierCategoryId { get; set; }
    public SupplierCategory? SupplierCategory { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<SupplierInvoiceType> SupplierInvoiceTypes { get; set; } = new List<SupplierInvoiceType>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
