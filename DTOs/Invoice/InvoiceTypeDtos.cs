using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceTypeResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InvoiceTypeDepartmentStepResponseDto> DepartmentSteps { get; set; } = new();
}

public class CreateInvoiceTypeRequestDto
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}

public class UpdateInvoiceTypeRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }
}
