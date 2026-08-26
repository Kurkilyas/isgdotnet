using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class PermissionListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class RolePermissionDto
{
    public int PermissionId { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }
    public int? GrantedBy { get; set; }
}

public class CreatePermissionRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = null!;
}

public class UpdatePermissionRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = null!;
}

public class SetPermissionActiveRequestDto
{
    [Required]
    public bool IsActive { get; set; }
}

public class AssignPermissionRequestDto
{
    [Required]
    [MinLength(1)]
    public List<int> PermissionIds { get; set; } = [];
}

public class IdNameDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}
