using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class WorkflowTransitionRuleResponseDto
{
    public int Id { get; set; }
    public int InvoiceTypeId { get; set; }
    public WorkflowActionType TriggerAction { get; set; }
    public int? SourceStepId { get; set; }
    public string? SourceStepName { get; set; }
    public int TargetStepId { get; set; }
    public string TargetStepName { get; set; } = null!;
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

    [Range(1, int.MaxValue)]
    public int Priority { get; set; } = 100;
}

public class UpdateWorkflowTransitionRuleRequestDto
{
    [Required]
    public WorkflowActionType TriggerAction { get; set; }

    public int? SourceStepId { get; set; }

    [Required]
    public int TargetStepId { get; set; }

    [Range(1, int.MaxValue)]
    public int Priority { get; set; } = 100;
}
