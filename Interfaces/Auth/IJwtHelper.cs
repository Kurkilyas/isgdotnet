using InvoiceTrackingSystemBackend.Entities.Auth;

namespace InvoiceTrackingSystemBackend.Interfaces.Auth;

public interface IJwtHelper
{
    Task<(string AccessToken, string RefreshToken, DateTime AccessExpiresAt)> GenerateTokensAsync(User user);
    string HashToken(string token);
}
