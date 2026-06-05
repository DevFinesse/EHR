using EHR.IdentityService.Application.Staff;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class PostgresStaffMetadataRepository : IStaffMetadataRepository
{
    private const string GlobalScope = "";
    private readonly string _connectionString;

    public PostgresStaffMetadataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scopes = ReadScopes(tenantId);
        return await db.Roles.AsNoTracking()
            .Where(row => scopes.Contains(row.Scope))
            .Select(row => row.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetDepartmentsAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scopes = ReadScopes(tenantId);
        return await db.Departments.AsNoTracking()
            .Where(row => scopes.Contains(row.Scope))
            .Select(row => row.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        return await db.Permissions.AsNoTracking().OrderBy(row => row.Name).Select(row => row.Name).ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRolePermissionsAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scopes = ReadScopes(tenantId);
        var rows = await db.RolePermissions.AsNoTracking()
            .Where(row => scopes.Contains(row.Scope))
            .OrderBy(row => row.RoleName)
            .ThenBy(row => row.PermissionName)
            .ToArrayAsync(cancellationToken);

        return rows
            .GroupBy(row => row.RoleName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group.Select(row => row.PermissionName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(permission => permission).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string?> NormalizeRoleAsync(string? tenantId, string role, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        await using var db = CreateDbContext();
        return await FindReadableRoleAsync(db, tenantId, role, cancellationToken) is { } row ? row.Name : null;
    }

    public async Task<string?> NormalizeDepartmentAsync(string? tenantId, string department, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(department))
        {
            return null;
        }

        await using var db = CreateDbContext();
        return await FindReadableDepartmentAsync(db, tenantId, department, cancellationToken) is { } row ? row.Name : null;
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsForRoleAsync(string? tenantId, string role, CancellationToken cancellationToken)
    {
        var normalizedRole = await NormalizeRoleAsync(tenantId, role, cancellationToken);
        if (normalizedRole is null)
        {
            return [];
        }

        await using var db = CreateDbContext();
        var scopes = ReadScopes(tenantId);
        return await db.RolePermissions.AsNoTracking()
            .Where(row => scopes.Contains(row.Scope) && row.RoleName == normalizedRole)
            .Select(row => row.PermissionName)
            .Distinct()
            .OrderBy(permission => permission)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<StaffMetadataMutationResult> CreateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeRequired(name, "Role name");
        if (normalizedName.Error is not null)
        {
            return StaffMetadataMutationResult.Failure(normalizedName.Error);
        }

        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        if (await FindRoleAsync(db, scope, normalizedName.Value!, cancellationToken) is not null)
        {
            return StaffMetadataMutationResult.Failure($"Role '{normalizedName.Value}' already exists in this scope.");
        }

        db.Roles.Add(new StaffRoleRow { Scope = scope, Name = normalizedName.Value!, Description = NormalizeOptional(description), IsSystem = scope == GlobalScope, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> UpdateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var role = await FindRoleAsync(db, scope, name, cancellationToken);
        if (role is null)
        {
            return StaffMetadataMutationResult.Failure($"Role '{name}' was not found in this scope.");
        }

        role.Description = NormalizeOptional(description);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> DeleteRoleAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var role = await FindRoleAsync(db, scope, name, cancellationToken);
        if (role is null)
        {
            return StaffMetadataMutationResult.Failure($"Role '{name}' was not found in this scope.");
        }

        if (role.IsSystem && !deleteSystem)
        {
            return StaffMetadataMutationResult.Failure($"Role '{role.Name}' is a system role and cannot be deleted without force.");
        }

        if (await db.StaffUsers.AnyAsync(staff => staff.Role == role.Name && (scope == GlobalScope || staff.TenantId == scope), cancellationToken))
        {
            return StaffMetadataMutationResult.Failure($"Role '{role.Name}' is assigned to one or more staff users.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> CreateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeRequired(name, "Department name");
        if (normalizedName.Error is not null)
        {
            return StaffMetadataMutationResult.Failure(normalizedName.Error);
        }

        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        if (await FindDepartmentAsync(db, scope, normalizedName.Value!, cancellationToken) is not null)
        {
            return StaffMetadataMutationResult.Failure($"Department '{normalizedName.Value}' already exists in this scope.");
        }

        db.Departments.Add(new StaffDepartmentRow { Scope = scope, Name = normalizedName.Value!, Description = NormalizeOptional(description), IsSystem = scope == GlobalScope, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> UpdateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var department = await FindDepartmentAsync(db, scope, name, cancellationToken);
        if (department is null)
        {
            return StaffMetadataMutationResult.Failure($"Department '{name}' was not found in this scope.");
        }

        department.Description = NormalizeOptional(description);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> DeleteDepartmentAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var department = await FindDepartmentAsync(db, scope, name, cancellationToken);
        if (department is null)
        {
            return StaffMetadataMutationResult.Failure($"Department '{name}' was not found in this scope.");
        }

        if (department.IsSystem && !deleteSystem)
        {
            return StaffMetadataMutationResult.Failure($"Department '{department.Name}' is a system department and cannot be deleted without force.");
        }

        if (await db.StaffUsers.AnyAsync(staff => staff.Department == department.Name && (scope == GlobalScope || staff.TenantId == scope), cancellationToken))
        {
            return StaffMetadataMutationResult.Failure($"Department '{department.Name}' is assigned to one or more staff users.");
        }

        db.Departments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> CreatePermissionAsync(string name, string? description, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeRequired(name, "Permission name");
        if (normalizedName.Error is not null)
        {
            return StaffMetadataMutationResult.Failure(normalizedName.Error);
        }

        await using var db = CreateDbContext();
        if (await FindPermissionAsync(db, normalizedName.Value!, cancellationToken) is not null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{normalizedName.Value}' already exists.");
        }

        db.Permissions.Add(new StaffPermissionRow { Name = normalizedName.Value!, Description = NormalizeOptional(description), IsSystem = false, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> UpdatePermissionAsync(string name, string? description, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var permission = await FindPermissionAsync(db, name, cancellationToken);
        if (permission is null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{name}' was not found.");
        }

        permission.Description = NormalizeOptional(description);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> DeletePermissionAsync(string name, bool deleteSystem, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var permission = await FindPermissionAsync(db, name, cancellationToken);
        if (permission is null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{name}' was not found.");
        }

        if (permission.IsSystem && !deleteSystem)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permission.Name}' is a system permission and cannot be deleted without force.");
        }

        if (await db.RolePermissions.AnyAsync(grant => grant.PermissionName == permission.Name, cancellationToken))
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permission.Name}' is granted to one or more roles.");
        }

        db.Permissions.Remove(permission);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> GrantPermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var role = await FindReadableRoleAsync(db, tenantId, roleName, cancellationToken);
        if (role is null)
        {
            return StaffMetadataMutationResult.Failure($"Role '{roleName}' was not found.");
        }

        var permission = await FindPermissionAsync(db, permissionName, cancellationToken);
        if (permission is null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permissionName}' was not found.");
        }

        var grantScope = role.Scope == GlobalScope && scope != GlobalScope ? scope : role.Scope;
        if (grantScope != role.Scope && await FindRoleAsync(db, grantScope, role.Name, cancellationToken) is null)
        {
            db.Roles.Add(new StaffRoleRow { Scope = grantScope, Name = role.Name, Description = role.Description, IsSystem = false, CreatedAt = DateTimeOffset.UtcNow });
        }

        var exists = await db.RolePermissions.AnyAsync(grant => grant.Scope == grantScope && grant.RoleName == role.Name && grant.PermissionName == permission.Name, cancellationToken);
        if (exists)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permission.Name}' is already granted to role '{role.Name}' in this scope.");
        }

        db.RolePermissions.Add(new StaffRolePermissionRow { Scope = grantScope, RoleName = role.Name, PermissionName = permission.Name, CreatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    public async Task<StaffMetadataMutationResult> RevokePermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var scope = WriteScope(tenantId);
        var role = await FindRoleAsync(db, scope, roleName, cancellationToken);
        if (role is null)
        {
            return StaffMetadataMutationResult.Failure($"Role '{roleName}' was not found in this scope.");
        }

        var permission = await FindPermissionAsync(db, permissionName, cancellationToken);
        if (permission is null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permissionName}' was not found.");
        }

        var grant = await db.RolePermissions.SingleOrDefaultAsync(existing => existing.Scope == scope && existing.RoleName == role.Name && existing.PermissionName == permission.Name, cancellationToken);
        if (grant is null)
        {
            return StaffMetadataMutationResult.Failure($"Permission '{permission.Name}' is not granted to role '{role.Name}' in this scope.");
        }

        db.RolePermissions.Remove(grant);
        await db.SaveChangesAsync(cancellationToken);
        return StaffMetadataMutationResult.Success();
    }

    private static async Task<StaffRoleRow?> FindReadableRoleAsync(IdentityDbContext db, string? tenantId, string name, CancellationToken cancellationToken)
    {
        var requested = name.Trim().ToLower();
        var scopes = ReadScopes(tenantId);
        return await db.Roles
            .Where(row => scopes.Contains(row.Scope) && row.Name.ToLower() == requested)
            .OrderByDescending(row => row.Scope != GlobalScope)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<StaffDepartmentRow?> FindReadableDepartmentAsync(IdentityDbContext db, string? tenantId, string name, CancellationToken cancellationToken)
    {
        var requested = name.Trim().ToLower();
        var scopes = ReadScopes(tenantId);
        return await db.Departments
            .Where(row => scopes.Contains(row.Scope) && row.Name.ToLower() == requested)
            .OrderByDescending(row => row.Scope != GlobalScope)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<StaffRoleRow?> FindRoleAsync(IdentityDbContext db, string scope, string name, CancellationToken cancellationToken)
    {
        var requested = name.Trim().ToLower();
        return await db.Roles.SingleOrDefaultAsync(row => row.Scope == scope && row.Name.ToLower() == requested, cancellationToken);
    }

    private static async Task<StaffDepartmentRow?> FindDepartmentAsync(IdentityDbContext db, string scope, string name, CancellationToken cancellationToken)
    {
        var requested = name.Trim().ToLower();
        return await db.Departments.SingleOrDefaultAsync(row => row.Scope == scope && row.Name.ToLower() == requested, cancellationToken);
    }

    private static async Task<StaffPermissionRow?> FindPermissionAsync(IdentityDbContext db, string name, CancellationToken cancellationToken)
    {
        var requested = name.Trim().ToLower();
        return await db.Permissions.SingleOrDefaultAsync(row => row.Name.ToLower() == requested, cancellationToken);
    }

    private static string[] ReadScopes(string? tenantId) =>
        string.IsNullOrWhiteSpace(tenantId) ? [GlobalScope] : [GlobalScope, tenantId.Trim()];

    private static string WriteScope(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? GlobalScope : tenantId.Trim();

    private static (string? Value, string? Error) NormalizeRequired(string value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return (null, $"{label} is required.");
        }

        return (value.Trim(), null);
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private IdentityDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(_connectionString).Options;
        return new IdentityDbContext(options);
    }
}
