using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Master data. Örn: MALZEME_FATURASI, HIZMET_FATURASI, DENETIM_BELGELENDIRME, EGITIM, NAKLIYE.</summary>
public class InvoiceType : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = null!;

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InvoiceTypeDepartmentStep> DepartmentSteps { get; set; } = new List<InvoiceTypeDepartmentStep>();
    public ICollection<SupplierInvoiceType> SupplierInvoiceTypes { get; set; } = new List<SupplierInvoiceType>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
