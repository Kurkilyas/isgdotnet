using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Enums;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class WorkflowStepDefinitionResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int OrderNo { get; set; }
    public StepKind StepKind { get; set; }
    public string? ResponsibleRoleType { get; set; }
    public decimal? MaxDurationDays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateWorkflowStepDefinitionRequestDto
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [Required, Range(1, int.MaxValue)]
    public int OrderNo { get; set; }

    public StepKind StepKind { get; set; } = StepKind.Fixed;

    [MaxLength(30)]
    public string? ResponsibleRoleType { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateWorkflowStepDefinitionRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Name { get; set; } = null!;

    [Required, Range(1, int.MaxValue)]
    public int OrderNo { get; set; }

    [MaxLength(30)]
    public string? ResponsibleRoleType { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; }
}
