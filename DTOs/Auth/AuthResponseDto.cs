namespace isgDotnet.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public DateTime AccessExpiresAt { get; set; }
    public UserListDto User { get; set; } = null!;
}

public class AuthSessionResult
{
    public AuthResponseDto Response { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime RefreshExpiresAt { get; set; }
}
