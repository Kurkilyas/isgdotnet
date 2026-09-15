using isgDotnet.Constants;
using System.ComponentModel.DataAnnotations;
using isgDotnet.Entities.Common;

namespace isgDotnet.Entities.Auth;

public class UserVerificationCode : ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(255)]
    public string CodeHash { get; set; } = null!;

    public VerificationPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
