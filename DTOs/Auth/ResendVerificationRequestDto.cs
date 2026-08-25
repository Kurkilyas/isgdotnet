using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class ResendVerificationRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = null!;
}
