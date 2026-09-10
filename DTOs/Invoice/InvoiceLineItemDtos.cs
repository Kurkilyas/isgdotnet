using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceLineItemResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string Description { get; set; } = null!;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? LineAmount { get; set; }
}

public class CreateInvoiceLineItemRequestDto
{
    /// <summary>Bağımsız POST için zorunlu. Fatura create gövdesindeki kalemlerde yok sayılır.</summary>
    public int InvoiceId { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? LineAmount { get; set; }
}

public class UpdateInvoiceLineItemRequestDto
{
    [Required, MaxLength(500)]
    public string Description { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal? Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? LineAmount { get; set; }
}
