using System.ComponentModel.DataAnnotations;
using isgDotnet.Entities.Common;

namespace isgDotnet.Entities.Auth;

public class RefreshToken : ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(255)]
    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public int? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
    public ICollection<RefreshToken> ReplacedTokens { get; set; } = new List<RefreshToken>();

    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
