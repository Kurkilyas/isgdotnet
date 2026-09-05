using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Invoice;

public class InvoiceAttachmentResponseDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string FileName { get; set; } = null!;
    public string NasRelativePath { get; set; } = null!;
    public string FileType { get; set; } = null!;
    public int? FileSizeBytes { get; set; }
    public string? ChecksumSha256 { get; set; }
    public int? UploadedByUserId { get; set; }
    public string? UploadedByUserFullName { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class CreateInvoiceAttachmentRequestDto
{
    [Required]
    public int InvoiceId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = null!;

    [Required, MaxLength(500)]
    public string NasRelativePath { get; set; } = null!;

    [MaxLength(30)]
    public string FileType { get; set; } = "SCANNED_SIGNED_PDF";

    [Range(1, int.MaxValue)]
    public int? FileSizeBytes { get; set; }

    [MaxLength(64)]
    public string? ChecksumSha256 { get; set; }
}
