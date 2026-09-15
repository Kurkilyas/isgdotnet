using System.ComponentModel.DataAnnotations;
using isgDotnet.Entities.Common;

namespace isgDotnet.Entities.Auth;

public class Role : ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Bu role sahip kullanıcı, kendi departmanındaki kullanıcı listesini görebilir (UserRead olmadan).</summary>
    public bool IsManager { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
