using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Bildirim / SLA mail logu.</summary>
public class InvoiceNotificationLog
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int? WorkflowStepId { get; set; }
    public InvoiceWorkflowStep? WorkflowStep { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? RecipientUserId { get; set; }

    [MaxLength(150)]
    public string RecipientEmail { get; set; } = null!;

    public NotificationType NotificationType { get; set; }

    public DateTime SentAt { get; set; }

    public bool IsSuccess { get; set; } = true;

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
}
