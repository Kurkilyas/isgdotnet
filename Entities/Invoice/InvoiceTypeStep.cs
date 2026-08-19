using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. Bir fatura türünün TÜM akışı (ERP kontrol, departman kontrolleri, denetçiler,
/// muhasebe, arşiv dahil) burada tek ve düz bir sıralı liste olarak tanımlanır.
/// Örn (Kalite Güvence): 1:Muhasebe, 2:Buse Özdemir, 3:Gökhan Zerin, 4:Muhasebe, 5:Murat Bolat(denetçi), 6:Ali Bey.
/// </summary>
public class InvoiceTypeStep : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int InvoiceTypeId { get; set; }
    public InvoiceType InvoiceType { get; set; } = null!;

    /// <summary>GERÇEK, boşluksuz sıra (1,2,3,4,5,6...) - bu fatura türü için akışın TEK kaynağı.</summary>
    public int StepOrder { get; set; }

    /// <summary>Ekranda gösterilecek adım adı, ör. "Muhasebe Kontrol", "Gökhan Zerin Kontrolü", "Murat Bolat Denetim".</summary>
    [MaxLength(150)]
    public string StepName { get; set; } = null!;

    /// <summary>
    /// Opsiyonel sistemsel etiket: ERP_CONTROL / ACCOUNTING / ARCHIVE / AUDITOR / CONTROL / DEPARTMENT.
    /// Backend'in özel davranış tetiklemesi gereken adımlar için kullanılır, zorunlu değil.
    /// </summary>
    [MaxLength(30)]
    public string? StepRoleTag { get; set; }

    /// <summary>SoftFK -> Auth DB departments.Id, cross-database, EF Core navigation yok.
    /// Adım kişiye özel değil, departman havuzuna açılacaksa dolu (bu durumda Approvers boş kalır).</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Bu adımın SLA süresi (iş günü).</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InvoiceWorkflowStep> WorkflowSteps { get; set; } = new List<InvoiceWorkflowStep>();
    public ICollection<WorkflowTransitionRule> SourceTransitionRules { get; set; } = new List<WorkflowTransitionRule>();
    public ICollection<WorkflowTransitionRule> TargetTransitionRules { get; set; } = new List<WorkflowTransitionRule>();

    /// <summary>Kişiye özel adımlarda onaylayabilecek aday(lar), öncelik sırasıyla.</summary>
    public ICollection<InvoiceTypeStepApprover> Approvers { get; set; } = new List<InvoiceTypeStepApprover>();
}
