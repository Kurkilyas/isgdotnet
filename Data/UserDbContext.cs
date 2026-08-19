using InvoiceTrackingSystemBackend.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace InvoiceTrackingSystemBackend.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<Department> Departments { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserFile> UserFiles { get; set; }
    public DbSet<UserVerificationCode> UserVerificationCodes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AuthActivityLog> AuthActivityLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("departments");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Roles)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);
            entity.Property(e => e.FailedLoginCount).HasDefaultValue(0);
            entity.Property(e => e.IsOutOfOffice).HasDefaultValue(false);

            entity.HasOne(e => e.Department)
                  .WithMany(d => d.Users)
                  .HasForeignKey(e => e.DepartmentId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("user_roles");
            entity.HasIndex(e => new { e.UserId, e.RoleId })
                  .IsUnique()
                  .HasFilter("[IsActive] = 1 AND [DeletedAt] IS NULL")
                  .HasDatabaseName("uq_user_role_active");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.AssignedByUser)
                  .WithMany(u => u.AssignedUserRoles)
                  .HasForeignKey(e => e.AssignedBy)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("role_permissions");
            entity.HasIndex(e => new { e.RoleId, e.PermissionId })
                  .IsUnique()
                  .HasFilter("[DeletedAt] IS NULL")
                  .HasDatabaseName("uq_role_permission_active");

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.RolePermissions)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Permission)
                  .WithMany(p => p.RolePermissions)
                  .HasForeignKey(e => e.PermissionId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.GrantedByUser)
                  .WithMany(u => u.GrantedRolePermissions)
                  .HasForeignKey(e => e.GrantedBy)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<UserFile>(entity =>
        {
            entity.ToTable("user_files");
            entity.HasIndex(e => new { e.UserId, e.FileKind })
                  .IsUnique()
                  .HasFilter("[IsCurrent] = 1 AND [DeletedAt] IS NULL")
                  .HasDatabaseName("uq_user_file_current");
            entity.Property(e => e.FileKind)
                  .HasConversion<string>()
                  .HasMaxLength(20);
            entity.Property(e => e.IsCurrent).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Files)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.UploadedByUser)
                  .WithMany(u => u.UploadedFiles)
                  .HasForeignKey(e => e.UploadedByUserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<UserVerificationCode>(entity =>
        {
            entity.ToTable("user_verification_codes");
            entity.HasIndex(e => new { e.UserId, e.Purpose, e.ExpiresAt })
                  .HasDatabaseName("ix_verification_lookup");
            entity.Property(e => e.Purpose)
                  .HasConversion<string>()
                  .HasMaxLength(30);
            entity.Property(e => e.AttemptCount).HasDefaultValue(0);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.VerificationCodes)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ReplacedByToken)
                  .WithMany(t => t.ReplacedTokens)
                  .HasForeignKey(e => e.ReplacedByTokenId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        modelBuilder.Entity<AuthActivityLog>(entity =>
        {
            entity.ToTable("auth_activity_logs");
            entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                  .HasDatabaseName("ix_auth_log_user_time");
            entity.HasIndex(e => e.ActivityType);
            entity.Property(e => e.ActivityType)
                  .HasConversion<string>()
                  .HasMaxLength(40);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ActivityLogs)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }
}
