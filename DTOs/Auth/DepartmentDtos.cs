using System.ComponentModel.DataAnnotations;

namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class DepartmentListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateDepartmentRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Description { get; set; }
}

public class UpdateDepartmentRequestDto
{
    [Required, MaxLength(50)]
    public string Name { get; set; } = null!;

    [MaxLength(100)]
    public string? Description { get; set; }
}

public class SetDepartmentActiveRequestDto
{
    [Required]
    public bool IsActive { get; set; }
}
