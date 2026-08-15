using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceWorkflowHistoryResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public WorkflowActionType ActionType { get; set; }
    public InvoiceStatus? FromStatus { get; set; }
    public InvoiceStatus ToStatus { get; set; }
    public int? ActorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
