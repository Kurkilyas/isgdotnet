using InvoiceTrackingSystemBackend.Common;
using InvoiceTrackingSystemBackend.DTOs.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<VerifyEmailResponseDto> VerifyEmailAsync(VerifyEmailRequestDto request);
    Task<AuthSessionResult> LoginAsync(LoginRequestDto request);
    Task<AuthSessionResult> RefreshAsync(string refreshToken);
    Task LogoutAsync(string? refreshToken);
    Task<PagedResult<UserListDto>> GetListAsync(int page = 1, int pageSize = 20);
    Task<UserListDto?> GetByIdAsync(int id);
}
