using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>SADECE durum/adım değişikliklerini (onay, red, iade) tutar. Append-only log, silinmez.</summary>
public class InvoiceWorkflowHistory
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public WorkflowActionType ActionType { get; set; }

    public InvoiceStatus? FromStatus { get; set; }

    public InvoiceStatus ToStatus { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok. Sistem işlemiyse null.</summary>
    public int? ActorUserId { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
