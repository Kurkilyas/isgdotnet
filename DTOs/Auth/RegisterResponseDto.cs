namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class RegisterResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public bool IsVerified { get; set; }
    public string Message { get; set; } = null!;
    public string? DevelopmentCode { get; set; }
}
