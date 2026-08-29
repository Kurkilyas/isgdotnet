using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierCategoryResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class CreateSupplierCategoryRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
}

public class UpdateSupplierCategoryRequestDto
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = null!;
}
