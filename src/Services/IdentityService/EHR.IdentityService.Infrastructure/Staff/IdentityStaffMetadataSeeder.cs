using EHR.SharedKernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Staff;

public static class IdentityStaffMetadataSeeder
{
    private static readonly string[] DefaultDepartments =
    [
        "Administration",
        "Billing",
        "Emergency",
        "Inpatient",
        "Internal Medicine",
        "Laboratory",
        "Maternity",
        "Nursing",
        "Outpatient",
        "Pediatrics",
        "Pharmacy",
        "Platform",
        "Public Health",
        "Radiology",
        "Records",
        "Surgery"
    ];

    public static async Task SeedAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new IdentityDbContext(options);
        var now = DateTimeOffset.UtcNow;

        foreach (var role in PlatformRoles.All)
        {
            if (!await db.Roles.AnyAsync(existing => existing.Scope == string.Empty && existing.Name == role, cancellationToken))
            {
                db.Roles.Add(new StaffRoleRow { Scope = string.Empty, Name = role, IsSystem = true, CreatedAt = now });
            }
        }

        foreach (var department in DefaultDepartments)
        {
            if (!await db.Departments.AnyAsync(existing => existing.Scope == string.Empty && existing.Name == department, cancellationToken))
            {
                db.Departments.Add(new StaffDepartmentRow { Scope = string.Empty, Name = department, IsSystem = true, CreatedAt = now });
            }
        }

        foreach (var permission in PlatformPermissions.All)
        {
            if (!await db.Permissions.AnyAsync(existing => existing.Name == permission, cancellationToken))
            {
                db.Permissions.Add(new StaffPermissionRow { Name = permission, IsSystem = true, CreatedAt = now });
            }
        }

        foreach (var (role, permissions) in RolePermissionMap.All)
        {
            foreach (var permission in permissions)
            {
                var exists = await db.RolePermissions.AnyAsync(existing => existing.Scope == string.Empty && existing.RoleName == role && existing.PermissionName == permission, cancellationToken);
                if (!exists)
                {
                    db.RolePermissions.Add(new StaffRolePermissionRow { Scope = string.Empty, RoleName = role, PermissionName = permission, CreatedAt = now });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
