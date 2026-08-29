using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Constants;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>
/// Durum değiştirmeyen, salt gözlemsel kullanıcı etkileşim logu (workflow history'den farklı, çok daha ince taneli).
/// Append-only log, silinmez.
/// </summary>
public class InvoiceActivityLog
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int? WorkflowStepId { get; set; }
    public InvoiceWorkflowStep? WorkflowStep { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? UserId { get; set; }

    public InvoiceActivityType ActivityType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; }
}
