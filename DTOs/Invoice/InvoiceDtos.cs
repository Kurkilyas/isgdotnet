using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Enums;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

/// <summary>Liste ekranı için özet DTO - sık kullanılan alanlar.</summary>
public class InvoiceListItemDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public DateOnly InvoiceDate { get; set; }
    public string VknTckn { get; set; } = null!;
    public string? SupplierName { get; set; }
    public string? InvoiceTypeName { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal? AmountTry { get; set; }
    public InvoiceStatus CurrentStatus { get; set; }
    public bool IsDuplicate { get; set; }

    /// <summary>Şu an bekleyen adımın bilgisi (completed_at IS NULL olan workflow step'ten türetilir).</summary>
    public string? CurrentStepName { get; set; }
    public int? CurrentStepAssignedUserId { get; set; }
    public DateTime? CurrentStepDueAt { get; set; }
    public bool? CurrentStepIsOverdue { get; set; }
}

/// <summary>Detay ekranı için tam DTO - kalemler, son workflow adımları ve ekler dahil.</summary>
public class InvoiceDetailDto
{
    public int Id { get; set; }
    public string? ExternalInvoiceId { get; set; }
    public string VknTckn { get; set; } = null!;
    public string InvoiceNumber { get; set; } = null!;
    public DateOnly InvoiceDate { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? CurrentAccountCode { get; set; }
    public int? ExchangeRateId { get; set; }
    public decimal? ExchangeRateToTry { get; set; }
    public decimal? AmountTry { get; set; }
    public string? ErpMatchStatus { get; set; }
    public DateTime? ErpCheckedAt { get; set; }
    public string? ErpSource { get; set; }
    public bool IsDuplicate { get; set; }
    public string? ErrorReason { get; set; }
    public int? InvoiceTypeId { get; set; }
    public string? InvoiceTypeName { get; set; }
    public decimal? PredictionConfidence { get; set; }
    public string? PredictionModelVersion { get; set; }
    public AssignmentMethod? AssignmentMethod { get; set; }
    public int? AssignedUserId { get; set; }
    public int? Auditor1UserId { get; set; }
    public int? Auditor2UserId { get; set; }
    public InvoiceStatus CurrentStatus { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<InvoiceLineItemResponseDto> LineItems { get; set; } = new();
    public List<InvoiceWorkflowStepResponseDto> WorkflowSteps { get; set; } = new();
    public List<InvoiceAttachmentResponseDto> Attachments { get; set; } = new();
    public List<InvoiceRelationResponseDto> Relations { get; set; } = new();
}

public class CreateInvoiceRequestDto
{
    [MaxLength(100)]
    public string? ExternalInvoiceId { get; set; }

    [Required, MaxLength(11)]
    public string VknTckn { get; set; } = null!;

    [Required, MaxLength(50)]
    public string InvoiceNumber { get; set; } = null!;

    [Required]
    public DateOnly InvoiceDate { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    public Currency Currency { get; set; } = Currency.Try;

    public int? SupplierId { get; set; }

    [MaxLength(50)]
    public string? CurrentAccountCode { get; set; }

    public string? RawPayloadJson { get; set; }

    public List<CreateInvoiceLineItemRequestDto> LineItems { get; set; } = new();
}

public class UpdateInvoiceRequestDto
{
    [Required]
    public int Id { get; set; }

    public int? SupplierId { get; set; }

    public int? InvoiceTypeId { get; set; }

    public AssignmentMethod? AssignmentMethod { get; set; }

    public int? AssignedUserId { get; set; }
    public int? Auditor1UserId { get; set; }
    public int? Auditor2UserId { get; set; }

    [MaxLength(500)]
    public string? ErrorReason { get; set; }
}
