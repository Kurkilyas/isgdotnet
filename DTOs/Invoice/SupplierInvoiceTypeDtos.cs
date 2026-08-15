using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierInvoiceTypeResponseDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public int InvoiceTypeId { get; set; }
    public string InvoiceTypeName { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class CreateSupplierInvoiceTypeRequestDto
{
    [Required]
    public int SupplierId { get; set; }

    [Required]
    public int InvoiceTypeId { get; set; }

    public bool IsActive { get; set; } = true;
}

public class UpdateSupplierInvoiceTypeRequestDto
{
    [Required]
    public int Id { get; set; }

    public bool IsActive { get; set; }
}
