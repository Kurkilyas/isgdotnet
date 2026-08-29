using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierListDto
{
    public int Id { get; set; }
    public string VknTckn { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? SupplierCategoryId { get; set; }
    public string? SupplierCategoryName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class SupplierResponseDto
{
    public int Id { get; set; }
    public string VknTckn { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int? SupplierCategoryId { get; set; }
    public string? SupplierCategoryName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<InvoiceTypeSummaryDto> InvoiceTypes { get; set; } = [];
}

public class CreateSupplierRequestDto
{
    [Required, MaxLength(11)]
    public string VknTckn { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public int? SupplierCategoryId { get; set; }
}

public class UpdateSupplierRequestDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = null!;

    public int? SupplierCategoryId { get; set; }
}
