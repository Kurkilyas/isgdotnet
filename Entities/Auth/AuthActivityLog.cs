using InvoiceTrackingSystemBackend.Constants;
using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Auth;

public class AuthActivityLog : ISoftDeletable
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public AuthActivityType ActivityType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
