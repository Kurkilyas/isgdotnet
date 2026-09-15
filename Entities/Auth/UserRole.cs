using isgDotnet.Entities.Common;

namespace isgDotnet.Entities.Auth;

public class UserRole : ISoftDeletable
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime? AssignedAt { get; set; }

    public int? AssignedBy { get; set; }
    public User? AssignedByUser { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
