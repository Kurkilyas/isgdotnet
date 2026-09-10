using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Invoice;

/// <summary>Dosya ekleri (NAS). Yanlış yüklenen dosya "silinmiş" işaretlenir, fiziksel kayıt/NAS dosyası durur.</summary>
public class InvoiceAttachment : ISoftDeletable
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    [MaxLength(255)]
    public string FileName { get; set; } = null!;

    /// <summary>Ör: 2026/08/INV_123.pdf - NAS üzerindeki bağıl dosya yolu.</summary>
    [MaxLength(500)]
    public string NasRelativePath { get; set; } = null!;

    [MaxLength(30)]
    public string FileType { get; set; } = "SCANNED_SIGNED_PDF";

    public int? FileSizeBytes { get; set; }

    /// <summary>Dosya bütünlüğünü doğrulamak için hash değeri.</summary>
    [MaxLength(64)]
    public string? ChecksumSha256 { get; set; }

    /// <summary>SoftFK -> Auth DB users.Id, cross-database, EF Core navigation yok.</summary>
    public int? UploadedByUserId { get; set; }

    /// <summary>Yüklendiği iş akışı adımı. Adım kapanınca ek silinemez.</summary>
    public int? WorkflowStepId { get; set; }
    public InvoiceWorkflowStep? WorkflowStep { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
