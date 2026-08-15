using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceTrackingSystemBackend.Entities.Common;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Master data. TÜM fatura türleri için ortak/sabit adımlar + departman zincirinin nereye oturacağını gösteren yer tutucu.
/// order_no ÜZERİNDEN gerçek ve TEK akış sırası burada tanımlanır, kodda ayrı bir sabit liste yoktur.
/// </summary>
public class WorkflowStepDefinition : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Adımın sistemsel/teknik kodu - backend bu koda göre davranış belirler.</summary>
    [MaxLength(30)]
    public string Code { get; set; } = null!;

    [MaxLength(150)]
    public string Name { get; set; } = null!;

    /// <summary>GERÇEK, BOŞLUKSUZ akış sırası (1,2,3...) - backend ORDER BY order_no ile ilerler.</summary>
    public int OrderNo { get; set; }

    public StepKind StepKind { get; set; } = StepKind.Fixed;

    /// <summary>ERP_CONTROL / AUDITOR1 / AUDITOR2 / ACCOUNTING / ARCHIVE / SYSTEM (yer tutucuda boş).</summary>
    [MaxLength(30)]
    public string? ResponsibleRoleType { get; set; }

    /// <summary>Bu adımın SLA süresi (iş günü). Yer tutucuda boş, süre invoice_type_department_steps'ten gelir.</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InvoiceWorkflowStep> WorkflowSteps { get; set; } = new List<InvoiceWorkflowStep>();
    public ICollection<WorkflowTransitionRule> SourceTransitionRules { get; set; } = new List<WorkflowTransitionRule>();
    public ICollection<WorkflowTransitionRule> TargetTransitionRules { get; set; } = new List<WorkflowTransitionRule>();
}
