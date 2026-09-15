using System.ComponentModel.DataAnnotations;

namespace isgDotnet.DTOs.Auth;

public class ResendVerificationRequestDto
{
    [Required]
    [EmailAddress]
    [MaxLength(100)]
    public string Email { get; set; } = null!;
}
