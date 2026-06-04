using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EHR.IdentityService.Infrastructure.Staff;

public static class IdentityDevelopmentSeeder
{
    public static async Task SeedAsync(string connectionString, IPasswordHasher passwordHasher, IConfiguration configuration, IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        var enabled = bool.TryParse(configuration["SeedAdmin:Enabled"], out var configuredEnabled)
            ? configuredEnabled
            : environment.IsDevelopment();
        if (!enabled)
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"] ?? "admin@ehr.local";
        var password = configuration["SeedAdmin:Password"] ?? "Admin123!ChangeMe";
        var tenantId = configuration["SeedAdmin:TenantId"] ?? "platform";
        var fullName = configuration["SeedAdmin:FullName"] ?? "Platform Super Admin";
        var role = configuration["SeedAdmin:Role"] ?? "Super Admin";
        var department = configuration["SeedAdmin:Department"] ?? "Platform";

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new IdentityDbContext(options);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var exists = await db.StaffUsers.AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            return;
        }

        var staffUser = StaffUser.Create(tenantId, fullName, normalizedEmail, role, department);
        staffUser.SetPassword(passwordHasher.Hash(password));

        db.StaffUsers.Add(new StaffUserRow
        {
            Id = staffUser.Id,
            TenantId = staffUser.TenantId,
            FullName = staffUser.FullName,
            Email = staffUser.Email,
            Role = staffUser.Role,
            Department = staffUser.Department,
            PasswordHash = staffUser.PasswordHash,
            MfaEnabled = staffUser.MfaEnabled,
            MfaSecret = staffUser.MfaSecret,
            RecoveryCodesHash = staffUser.RecoveryCodesHash,
            FailedLoginAttempts = staffUser.FailedLoginAttempts,
            LockedUntil = staffUser.LockedUntil,
            CreatedAt = staffUser.CreatedAt
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
