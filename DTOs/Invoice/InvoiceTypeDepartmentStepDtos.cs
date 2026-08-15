using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceTypeDepartmentStepResponseDto
{
    public int Id { get; set; }
    public int InvoiceTypeId { get; set; }
    public int StepOrder { get; set; }
    public int? DepartmentId { get; set; }
    public int? DefaultUserId { get; set; }
    public string? DefaultUserDisplayName { get; set; }
    public decimal? MaxDurationDays { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInvoiceTypeDepartmentStepRequestDto
{
    [Required]
    public int InvoiceTypeId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int StepOrder { get; set; }

    public int? DepartmentId { get; set; }
    public int? DefaultUserId { get; set; }

    [MaxLength(150)]
    public string? DefaultUserDisplayName { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateInvoiceTypeDepartmentStepRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int StepOrder { get; set; }

    public int? DepartmentId { get; set; }
    public int? DefaultUserId { get; set; }

    [MaxLength(150)]
    public string? DefaultUserDisplayName { get; set; }

    [Range(0.01, 999.99)]
    public decimal? MaxDurationDays { get; set; }

    public bool IsActive { get; set; }
}
