using System.ComponentModel.DataAnnotations;
using InvoiceTrackingSystemBackend.Entities.Common;

namespace InvoiceTrackingSystemBackend.Entities.Auth;

public class User : IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(100)]
    public string Email { get; set; } = null!;

    [MaxLength(255)]
    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>DB kolonu durur; API yazmaz. Cevaplardaki <c>position</c> aktif rollerin DisplayName birleşimidir.</summary>
    [MaxLength(100)]
    public string? Position { get; set; }

    /// <summary>Kullanıcı şu an izinli/müsait değil mi. Workflow atama motoru bu bayrağa bakarak
    /// InvoiceTypeStepApprover zincirinde bir sonraki öncelikli kişiye otomatik geçer.</summary>
    public bool IsOutOfOffice { get; set; }

    /// <summary>İzin/müsaitsizlik bitiş tahmini (bilgi amaçlı).</summary>
    public DateTime? OutOfOfficeUntil { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserRole> AssignedUserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> GrantedRolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserFile> Files { get; set; } = new List<UserFile>();
    public ICollection<UserFile> UploadedFiles { get; set; } = new List<UserFile>();
    public ICollection<UserVerificationCode> VerificationCodes { get; set; } = new List<UserVerificationCode>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<AuthActivityLog> ActivityLogs { get; set; } = new List<AuthActivityLog>();
}
