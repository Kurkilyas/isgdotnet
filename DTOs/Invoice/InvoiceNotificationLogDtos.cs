using InvoiceTrackingSystemBackend.Enums;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceNotificationLogResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? WorkflowStepId { get; set; }
    public int? RecipientUserId { get; set; }
    public string RecipientEmail { get; set; } = null!;
    public NotificationType NotificationType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
