using System.ComponentModel.DataAnnotations;

namespace isgDotnet.DTOs.Auth;

public class VerifyEmailRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = null!;
}
