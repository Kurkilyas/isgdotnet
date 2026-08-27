namespace InvoiceTrackingSystemBackend.DTOs.Auth;

public class UserListDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Phone { get; set; }
    public string? Position { get; set; }
    public bool IsOutOfOffice { get; set; }
    public DateTime? OutOfOfficeUntil { get; set; }
    public DateTime? CreatedAt { get; set; }
}
