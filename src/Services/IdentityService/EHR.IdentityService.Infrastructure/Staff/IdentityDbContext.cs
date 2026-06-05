using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<StaffUserRow> StaffUsers => Set<StaffUserRow>();
    public DbSet<RefreshTokenRow> RefreshTokens => Set<RefreshTokenRow>();
    public DbSet<StaffInvitationRow> StaffInvitations => Set<StaffInvitationRow>();
    public DbSet<PasswordResetTokenRow> PasswordResetTokens => Set<PasswordResetTokenRow>();
    public DbSet<TenantRegistrationRow> TenantRegistrations => Set<TenantRegistrationRow>();
    public DbSet<StaffRoleRow> Roles => Set<StaffRoleRow>();
    public DbSet<StaffDepartmentRow> Departments => Set<StaffDepartmentRow>();
    public DbSet<StaffPermissionRow> Permissions => Set<StaffPermissionRow>();
    public DbSet<StaffRolePermissionRow> RolePermissions => Set<StaffRolePermissionRow>();
    public DbSet<IdentityOutboxMessageRow> OutboxMessages => Set<IdentityOutboxMessageRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StaffUserRow>(entity =>
        {
            entity.ToTable("staff_users");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.Email).IsUnique();
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.FullName).HasColumnName("full_name");
            entity.Property(row => row.Email).HasColumnName("email");
            entity.Property(row => row.Role).HasColumnName("role");
            entity.Property(row => row.Department).HasColumnName("department");
            entity.Property(row => row.PasswordHash).HasColumnName("password_hash");
            entity.Property(row => row.MfaEnabled).HasColumnName("mfa_enabled");
            entity.Property(row => row.MfaSecret).HasColumnName("mfa_secret");
            entity.Property(row => row.RecoveryCodesHash).HasColumnName("recovery_codes_hash");
            entity.Property(row => row.FailedLoginAttempts).HasColumnName("failed_login_attempts");
            entity.Property(row => row.LockedUntil).HasColumnName("locked_until");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<RefreshTokenRow>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(row => row.Token);
            entity.Property(row => row.Token).HasColumnName("token");
            entity.Property(row => row.StaffUserId).HasColumnName("staff_user_id");
            entity.Property(row => row.ExpiresAt).HasColumnName("expires_at");
            entity.Property(row => row.RevokedAt).HasColumnName("revoked_at");
        });

        modelBuilder.Entity<StaffInvitationRow>(entity =>
        {
            entity.ToTable("staff_invitations");
            entity.HasKey(row => row.Token);
            entity.Property(row => row.Token).HasColumnName("token");
            entity.Property(row => row.StaffUserId).HasColumnName("staff_user_id");
            entity.Property(row => row.ExpiresAt).HasColumnName("expires_at");
            entity.Property(row => row.AcceptedAt).HasColumnName("accepted_at");
        });

        modelBuilder.Entity<PasswordResetTokenRow>(entity =>
        {
            entity.ToTable("password_reset_tokens");
            entity.HasKey(row => row.Token);
            entity.Property(row => row.Token).HasColumnName("token");
            entity.Property(row => row.StaffUserId).HasColumnName("staff_user_id");
            entity.Property(row => row.ExpiresAt).HasColumnName("expires_at");
            entity.Property(row => row.UsedAt).HasColumnName("used_at");
        });

        modelBuilder.Entity<TenantRegistrationRow>(entity =>
        {
            entity.ToTable("tenant_registrations");
            entity.HasKey(row => row.TenantId);
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.HospitalId).HasColumnName("hospital_id");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.RegisteredAt).HasColumnName("registered_at");
            entity.Property(row => row.CorrelationId).HasColumnName("correlation_id");
        });

        modelBuilder.Entity<StaffRoleRow>(entity =>
        {
            entity.ToTable("staff_roles");
            entity.HasKey(row => new { row.Scope, row.Name });
            entity.Property(row => row.Scope).HasColumnName("scope");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.Description).HasColumnName("description");
            entity.Property(row => row.IsSystem).HasColumnName("is_system");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<StaffDepartmentRow>(entity =>
        {
            entity.ToTable("staff_departments");
            entity.HasKey(row => new { row.Scope, row.Name });
            entity.Property(row => row.Scope).HasColumnName("scope");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.Description).HasColumnName("description");
            entity.Property(row => row.IsSystem).HasColumnName("is_system");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<StaffPermissionRow>(entity =>
        {
            entity.ToTable("staff_permissions");
            entity.HasKey(row => row.Name);
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.Description).HasColumnName("description");
            entity.Property(row => row.IsSystem).HasColumnName("is_system");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<StaffRolePermissionRow>(entity =>
        {
            entity.ToTable("staff_role_permissions");
            entity.HasKey(row => new { row.Scope, row.RoleName, row.PermissionName });
            entity.Property(row => row.Scope).HasColumnName("scope");
            entity.Property(row => row.RoleName).HasColumnName("role_name");
            entity.Property(row => row.PermissionName).HasColumnName("permission_name");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
            entity.HasOne<StaffRoleRow>()
                .WithMany()
                .HasForeignKey(row => new { row.Scope, row.RoleName })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<StaffPermissionRow>()
                .WithMany()
                .HasForeignKey(row => row.PermissionName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdentityOutboxMessageRow>(entity =>
        {
            entity.MapOutboxMessage();
        });
    }
}

public sealed class StaffUserRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public string? RecoveryCodesHash { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RefreshTokenRow
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class StaffInvitationRow
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}

public sealed class PasswordResetTokenRow
{
    public string Token { get; set; } = string.Empty;
    public Guid StaffUserId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}

public sealed class TenantRegistrationRow
{
    public string TenantId { get; set; } = string.Empty;
    public Guid HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class StaffRoleRow
{
    public string Scope { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StaffDepartmentRow
{
    public string Scope { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StaffPermissionRow
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class StaffRolePermissionRow
{
    public string Scope { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string PermissionName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class IdentityOutboxMessageRow : OutboxMessageRowBase
{
    public static IdentityOutboxMessageRow FromIntegrationEvent(IntegrationEvent integrationEvent)
    {
        var row = new IdentityOutboxMessageRow();
        row.Apply(integrationEvent);
        return row;
    }
}
