using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Sadece sınıflandırma/raporlama amaçlı (departman ataması YAPMAZ). Örn: Belgelendirme Kuruluşu, Nakliye Firması.</summary>
public class SupplierCategory : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
