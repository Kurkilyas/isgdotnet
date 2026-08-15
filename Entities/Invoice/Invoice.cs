using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InvoiceTrackingSystemBackend.Entities.Common;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Fatura ana kaydı. Mali/denetim kaydı olduğu için fiziksel DELETE asla yapılmaz.
/// "Şu an hangi adımda" bilgisi burada TUTULMAZ; her zaman invoice_workflow_steps'ten
/// (completed_at IS NULL olan satır) canlı sorguyla türetilir - çifte kaynak riski yaratmamak için.
/// </summary>
public class Invoice : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    /// <summary>Faturanın kaynak sistemdeki (harici web servisindeki) kimliği.</summary>
    [MaxLength(100)]
    public string? ExternalInvoiceId { get; set; }

    /// <summary>Faturayı kesen tarafın Vergi/TC Kimlik No'su.</summary>
    [MaxLength(11)]
    public string VknTckn { get; set; } = null!;

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    /// <summary>Faturanın orijinal (kesildiği para biriminde) tutarı.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; } = Currency.Try;

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>ERP cari kodu - sadece cari kimliği, fatura türünü AYIRT ETMEZ.</summary>
    [MaxLength(50)]
    public string? CurrentAccountCode { get; set; }

    /// <summary>Harici servisten gelen ham veri (JSON), denetim izi/geri dönüş amaçlı saklanır.</summary>
    public string? RawPayloadJson { get; set; }

    /// <summary>SNAPSHOT -> exchange_rates.id, sabit kalır.</summary>
    public int? ExchangeRateId { get; set; }
    public ExchangeRate? ExchangeRate { get; set; }

    /// <summary>Amount * ExchangeRate.RateToTry.</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AmountTry { get; set; }

    /// <summary>MATCHED / NOT_FOUND / MISMATCH</summary>
    [MaxLength(30)]
    public string? ErpMatchStatus { get; set; }

    public DateTime? ErpCheckedAt { get; set; }

    /// <summary>LOCAL_DB / ERP_SERVER / BOTH</summary>
    [MaxLength(30)]
    public string? ErpSource { get; set; }

    /// <summary>Hızlı filtre için denormalize alan; detay invoice_relations tablosunda.</summary>
    public bool IsDuplicate { get; set; }

    [MaxLength(500)]
    public string? ErrorReason { get; set; }

    /// <summary>ML ile veya tek adaylı cari ise otomatik belirlenir.</summary>
    public int? InvoiceTypeId { get; set; }
    public InvoiceType? InvoiceType { get; set; }

    /// <summary>0.0000 - 1.0000 arası ML güven skoru.</summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal? PredictionConfidence { get; set; }

    [MaxLength(50)]
    public string? PredictionModelVersion { get; set; }

    public AssignmentMethod? AssignmentMethod { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? AssignedUserId { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? Auditor1UserId { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? Auditor2UserId { get; set; }

    /// <summary>Faturanın o anki genel durumu (dashboard/filtreleme için). Adım bazında detay için InvoiceWorkflowSteps sorgulanır.</summary>
    public InvoiceStatus CurrentStatus { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<InvoiceRelation> Relations { get; set; } = new List<InvoiceRelation>();
    public ICollection<InvoiceRelation> RelatedToRelations { get; set; } = new List<InvoiceRelation>();
    public ICollection<InvoiceWorkflowStep> WorkflowSteps { get; set; } = new List<InvoiceWorkflowStep>();
    public ICollection<InvoiceWorkflowHistory> WorkflowHistories { get; set; } = new List<InvoiceWorkflowHistory>();
    public ICollection<InvoiceActivityLog> ActivityLogs { get; set; } = new List<InvoiceActivityLog>();
    public ICollection<InvoiceAttachment> Attachments { get; set; } = new List<InvoiceAttachment>();
    public ICollection<InvoiceNotificationLog> NotificationLogs { get; set; } = new List<InvoiceNotificationLog>();
}
