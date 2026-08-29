using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceTypeStepResponseDto
{
    public int Id { get; set; }
    public int InvoiceTypeId { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = null!;
    public string? StepRoleTag { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public decimal? MaxDurationDays { get; set; }
    public bool IsActive { get; set; }
    public List<InvoiceTypeStepApproverResponseDto> Approvers { get; set; } = [];
}

public class CreateInvoiceTypeStepRequestDto
{
    [Required, Range(1, int.MaxValue)]
    public int StepOrder { get; set; }

    [Required, MaxLength(150)]
    public string StepName { get; set; } = null!;

    [MaxLength(30)]
    public string? StepRoleTag { get; set; }

    public int? DepartmentId { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }

    /// <summary>Kişiye özel adımlarda öncelik sıralı onaylayıcı adayları. DepartmentId doluysa boş bırakılmalı.</summary>
    public List<CreateInvoiceTypeStepApproverRequestDto> Approvers { get; set; } = [];
}

public class UpdateInvoiceTypeStepRequestDto
{
    [Required, Range(1, int.MaxValue)]
    public int StepOrder { get; set; }

    [Required, MaxLength(150)]
    public string StepName { get; set; } = null!;

    [MaxLength(30)]
    public string? StepRoleTag { get; set; }

    public int? DepartmentId { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }
}
