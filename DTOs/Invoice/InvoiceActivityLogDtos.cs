using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceActivityLogResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? WorkflowStepId { get; set; }
    public int? UserId { get; set; }
    public ActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInvoiceActivityLogRequestDto
{
    [Required]
    public int InvoiceId { get; set; }

    public int? WorkflowStepId { get; set; }

    [Required]
    public ActivityType ActivityType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
