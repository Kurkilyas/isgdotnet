using InvoiceTrackingSystemBackend.Constants;
using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Auth;

public class UserFile : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public UserFileKind FileKind { get; set; }

    [MaxLength(255)]
    public string OriginalFileName { get; set; } = null!;

    [MaxLength(500)]
    public string NasRelativePath { get; set; } = null!;

    [MaxLength(50)]
    public string ContentType { get; set; } = null!;

    public int? FileSizeBytes { get; set; }

    [MaxLength(64)]
    public string? ChecksumSha256 { get; set; }

    public bool IsCurrent { get; set; } = true;

    public int? UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
