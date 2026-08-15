using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Bir adımdan HER geçişte yeni bir satır açılır (iade/tekrar durumunda aynı adım birden fazla kez görünebilir).
/// Append-only log, silinmez.
/// EŞZAMANLILIK: adım tamamlanırken UPDATE ... WHERE id=@id AND completed_at IS NULL kullanılmalı;
/// 0 satır etkilenirse adım başkası tarafından zaten işlenmiştir (RowVersion gerekmez, completed_at bu görevi görür).
/// step_definition_id ve department_step_id alanlarından TAM OLARAK BİRİ dolu olmalıdır (CHECK constraint).
/// </summary>
public class InvoiceWorkflowStep
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    /// <summary>Sabit adımlarda dolu, departman adımında NULL.</summary>
    public int? StepDefinitionId { get; set; }
    public WorkflowStepDefinition? StepDefinition { get; set; }

    /// <summary>Departman zinciri adımlarında dolu, sabit adımda NULL.</summary>
    public int? DepartmentStepId { get; set; }
    public InvoiceTypeDepartmentStep? DepartmentStep { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? AssignedUserId { get; set; }

    /// <summary>SoftFK -> Auth DB departments.Id, cross-database, EF Core navigation yok (denormalize, hızlı filtre için).</summary>
    public int? AssignedDepartmentId { get; set; }

    public DateTime StartedAt { get; set; }

    /// <summary>SLA bitiş zamanı.</summary>
    public DateTime? DueAt { get; set; }

    /// <summary>Boşsa adım hâlâ bekliyor demektir.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Hatırlatma maili gönderildiyse ne zaman gönderildiği (tekrar göndermemek için).</summary>
    public DateTime? ReminderSentAt { get; set; }

    public bool IsOverdue { get; set; }

    /// <summary>APPROVED / REJECTED / MISSING_DOCUMENT</summary>
    [MaxLength(30)]
    public string? Result { get; set; }

    [MaxLength(500)]
    public string? RejectReason { get; set; }

    public ICollection<InvoiceActivityLog> ActivityLogs { get; set; } = new List<InvoiceActivityLog>();
    public ICollection<InvoiceNotificationLog> NotificationLogs { get; set; } = new List<InvoiceNotificationLog>();
}
