using isgDotnet.Constants;

namespace isgDotnet.DTOs.Auth;

public class UserActivityLogDto
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public AuthActivityType ActivityType { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}
