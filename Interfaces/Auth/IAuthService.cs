using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<VerifyEmailResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request);
    Task<AuthSessionResult> LoginAsync(LoginRequestDto request);
    Task<AuthSessionResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string? refreshToken);
}
