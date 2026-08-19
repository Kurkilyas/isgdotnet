using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class WorkflowTransitionRuleResponseDto
{
    public int Id { get; set; }
    public WorkflowActionType TriggerAction { get; set; }
    public int? SourceStepId { get; set; }
    public int TargetStepId { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkflowTransitionRuleRequestDto
{
    [Required]
    public WorkflowActionType TriggerAction { get; set; }

    public int? SourceStepId { get; set; }

    [Required]
    public int TargetStepId { get; set; }

    public int Priority { get; set; } = 100;

    public bool IsActive { get; set; } = true;
}

public class UpdateWorkflowTransitionRuleRequestDto
{
    [Required]
    public int Id { get; set; }

    public int? SourceStepId { get; set; }

    [Required]
    public int TargetStepId { get; set; }

    public int Priority { get; set; }

    public bool IsActive { get; set; }
}
