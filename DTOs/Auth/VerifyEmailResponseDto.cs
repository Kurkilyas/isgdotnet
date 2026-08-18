namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class VerifyEmailResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public bool IsVerified { get; set; }
}
