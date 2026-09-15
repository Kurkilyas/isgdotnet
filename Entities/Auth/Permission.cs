using System.ComponentModel.DataAnnotations;
using isgDotnet.Entities.Common;

namespace isgDotnet.Entities.Auth;

public class Permission : ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string DisplayName { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
