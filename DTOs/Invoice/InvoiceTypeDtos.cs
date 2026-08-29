using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceTypeListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public int StepCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class InvoiceTypeResponseDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InvoiceTypeStepResponseDto> Steps { get; set; } = [];
}

public class InvoiceTypeSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateInvoiceTypeRequestDto
{
    [Required, MaxLength(30)]
    public string Code { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
}

public class UpdateInvoiceTypeRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
}
