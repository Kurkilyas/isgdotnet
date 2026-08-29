using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceTypeStepApproverResponseDto
{
    public int Id { get; set; }
    public int InvoiceTypeStepId { get; set; }
    public int UserId { get; set; }
    public string? UserFullName { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInvoiceTypeStepApproverRequestDto
{
    [Required]
    public int UserId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Priority { get; set; }
}

public class UpdateInvoiceTypeStepApproverRequestDto
{
    [Required, Range(1, int.MaxValue)]
    public int Priority { get; set; }
}
