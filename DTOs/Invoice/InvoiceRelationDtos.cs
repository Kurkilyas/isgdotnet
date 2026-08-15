using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Enums;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceRelationResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public int RelatedInvoiceId { get; set; }
    public string RelatedInvoiceNumber { get; set; } = null!;
    public InvoiceRelationType RelationType { get; set; }
    public string? Note { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInvoiceRelationRequestDto
{
    [Required]
    public int InvoiceId { get; set; }

    [Required]
    public int RelatedInvoiceId { get; set; }

    [Required]
    public InvoiceRelationType RelationType { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}
