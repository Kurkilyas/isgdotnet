using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class RoleListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsManager { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}

public class UserRoleDto
{
    public int RoleId { get; set; }
    public string Name { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public bool IsManager { get; set; }
    public DateTime? AssignedAt { get; set; }
    public int? AssignedBy { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class AssignRoleRequestDto
{
    [Required]
    public int RoleId { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

public class CreateRoleRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsManager { get; set; }

    public int? DepartmentId { get; set; }
}

public class UpdateRoleRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsManager { get; set; }

    public int? DepartmentId { get; set; }
}

public class SetRoleActiveRequestDto
{
    [Required]
    public bool IsActive { get; set; }
}
