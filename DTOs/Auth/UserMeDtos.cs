using System.ComponentModel.DataAnnotations;
using isgDotnet.Constants;

namespace isgDotnet.DTOs.Auth;

public class UserMeDto
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
    public bool HasProfilePhoto { get; set; }
    public bool HasSignature { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class UpdateMeRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(20)]
    public string? Phone { get; set; }
}

public class UpdateUserRequestDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [Required]
    public bool IsOutOfOffice { get; set; }

    public DateTime? OutOfOfficeUntil { get; set; }
}

public class SetUserActiveRequestDto
{
    [Required]
    public bool IsActive { get; set; }
}

public class ChangePasswordRequestDto
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; set; } = null!;
}

public class UpdateOutOfOfficeRequestDto
{
    [Required]
    public bool IsOutOfOffice { get; set; }

    public DateTime? OutOfOfficeUntil { get; set; }
}

public class UserFileMetaDto
{
    public UserFileKind FileKind { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public int? FileSizeBytes { get; set; }
    public DateTime? UploadedAt { get; set; }
}
