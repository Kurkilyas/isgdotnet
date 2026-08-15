using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class WorkflowTransitionRuleResponseDto
{
    public int Id { get; set; }
    public WorkflowActionType TriggerAction { get; set; }
    public string? SourceStepCode { get; set; }
    public string TargetStepCode { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkflowTransitionRuleRequestDto
{
    [Required]
    public WorkflowActionType TriggerAction { get; set; }

    [MaxLength(30)]
    public string? SourceStepCode { get; set; }

    [Required, MaxLength(30)]
    public string TargetStepCode { get; set; } = null!;

    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}

public class UpdateWorkflowTransitionRuleRequestDto
{
    [Required]
    public int Id { get; set; }

    [MaxLength(30)]
    public string? SourceStepCode { get; set; }

    [Required, MaxLength(30)]
    public string TargetStepCode { get; set; } = null!;

    public int Priority { get; set; }

    public bool IsActive { get; set; }
}
