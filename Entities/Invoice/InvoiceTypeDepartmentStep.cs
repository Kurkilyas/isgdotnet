using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. Örn: MALZEME_FATURASI -> [1:Satınalma, 2:Depo]. Fatura türüne göre SABİT ve önceden bellidir.
/// </summary>
public class InvoiceTypeDepartmentStep : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int InvoiceTypeId { get; set; }
    public InvoiceType InvoiceType { get; set; } = null!;

    /// <summary>1, 2, 3... zincirdeki sırası.</summary>
    public int StepOrder { get; set; }

    /// <summary>SoftFK -> Auth DB departments.Id, cross-database, EF Core navigation yok.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok. Departman içinde bu adımın SABİT sorumlusu belliyse dolu.</summary>
    public int? DefaultUserId { get; set; }

    /// <summary>
    /// CACHE/SNAPSHOT amaçlı - Auth DB'deki gerçek isim değişirse eskiyebilir (stale).
    /// Sadece hızlı görüntüleme için, yetkilendirme/iş mantığında KULLANILMAZ.
    /// </summary>
    [MaxLength(150)]
    public string? DefaultUserDisplayName { get; set; }

    /// <summary>Bu departman adımının SLA süresi (iş günü).</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InvoiceWorkflowStep> WorkflowSteps { get; set; } = new List<InvoiceWorkflowStep>();
}
