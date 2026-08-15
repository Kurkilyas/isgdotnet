using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierCategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateSupplierCategoryRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierCategoryRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }
}
