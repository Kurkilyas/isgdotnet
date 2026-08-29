using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class SupplierInvoiceTypeResponseDto
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = null!;
    public int InvoiceTypeId { get; set; }
    public string InvoiceTypeCode { get; set; } = null!;
    public string InvoiceTypeName { get; set; } = null!;
    public bool IsActive { get; set; }
}

public class AssignSupplierInvoiceTypesRequestDto
{
    [Required]
    [MinLength(1)]
    public List<int> InvoiceTypeIds { get; set; } = [];
}
