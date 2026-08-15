using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierResponseDto
{
    public int Id { get; set; }
    public string VknTckn { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? SupplierCategoryId { get; set; }
    public string? SupplierCategoryName { get; set; }
    public bool IsActive { get; set; }
    public List<InvoiceTypeResponseDto> InvoiceTypes { get; set; } = new();
}

public class CreateSupplierRequestDto
{
    [Required, MaxLength(11)]
    public string VknTckn { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public int? SupplierCategoryId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public int? SupplierCategoryId { get; set; }

    public bool IsActive { get; set; }
}
