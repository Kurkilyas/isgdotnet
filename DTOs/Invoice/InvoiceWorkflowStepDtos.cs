using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceWorkflowStepResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int InvoiceTypeStepId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = null!;
    public int? AssignedUserId { get; set; }
    public string? AssignedUserFullName { get; set; }
    public int? AssignedDepartmentId { get; set; }
    public string? AssignedDepartmentName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ReminderSentAt { get; set; }
    public bool IsOverdue { get; set; }
    public string? Result { get; set; }
    public string? RejectReason { get; set; }
}

/// <summary>Workflow motorunun onay endpoint'i için aksiyon DTO'su.</summary>
public class ApproveStepRequestDto
{
    [Required]
    public int StepId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

/// <summary>Workflow motorunun red endpoint'i için aksiyon DTO'su.</summary>
public class RejectStepRequestDto
{
    [Required]
    public int StepId { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = null!;
}

/// <summary>Eksik evrak bildirimi için aksiyon DTO'su.</summary>
public class FlagMissingDocumentRequestDto
{
    [Required]
    public int StepId { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = null!;
}
