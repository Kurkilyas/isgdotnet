using isgDotnet.Entities.Auth;

namespace isgDotnet.Interfaces.Auth;

public interface IJwtHelper
{
    Task<(string AccessToken, string RefreshToken, DateTime AccessExpiresAt)> GenerateTokensAsync(User user);
    string HashToken(string token);
}
