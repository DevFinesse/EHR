using EHR.IdentityService.Application.Staff;
using EHR.SharedKernel.Authorization;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class InMemoryStaffMetadataRepository : IStaffMetadataRepository
{
    private readonly Dictionary<string, string?> _roles;
    private readonly Dictionary<string, string?> _departments;
    private readonly Dictionary<string, string?> _permissions;
    private readonly Dictionary<string, HashSet<string>> _rolePermissions;

    public InMemoryStaffMetadataRepository()
    {
        _roles = PlatformRoles.All.ToDictionary(role => role, _ => (string?)null, StringComparer.OrdinalIgnoreCase);
        _departments = DefaultDepartments.ToDictionary(department => department, _ => (string?)null, StringComparer.OrdinalIgnoreCase);
        _permissions = PlatformPermissions.All.ToDictionary(permission => permission, _ => (string?)null, StringComparer.OrdinalIgnoreCase);
        _rolePermissions = RolePermissionMap.All.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToHashSet(StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

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

    public Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyCollection<string>)_roles.Keys.OrderBy(name => name).ToArray());

    public Task<IReadOnlyCollection<string>> GetDepartmentsAsync(string? tenantId, CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyCollection<string>)_departments.Keys.OrderBy(name => name).ToArray());

    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyCollection<string>)_permissions.Keys.OrderBy(name => name).ToArray());

    public Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRolePermissionsAsync(string? tenantId, CancellationToken cancellationToken)
    {
        var grants = _rolePermissions.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<string>)pair.Value.OrderBy(permission => permission).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        return Task.FromResult((IReadOnlyDictionary<string, IReadOnlyCollection<string>>)grants);
    }

    public Task<string?> NormalizeRoleAsync(string? tenantId, string role, CancellationToken cancellationToken) =>
        Task.FromResult(Normalize(role, _roles.Keys));

    public Task<string?> NormalizeDepartmentAsync(string? tenantId, string department, CancellationToken cancellationToken) =>
        Task.FromResult(Normalize(department, _departments.Keys));

    public Task<IReadOnlyCollection<string>> GetPermissionsForRoleAsync(string? tenantId, string role, CancellationToken cancellationToken)
    {
        var normalizedRole = Normalize(role, _roles.Keys);
        if (normalizedRole is null || !_rolePermissions.TryGetValue(normalizedRole, out var permissions))
        {
            return Task.FromResult((IReadOnlyCollection<string>)[]);
        }

        return Task.FromResult((IReadOnlyCollection<string>)permissions.OrderBy(permission => permission).ToArray());
    }

    public Task<StaffMetadataMutationResult> CreateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Create(name, description, _roles, () => _rolePermissions[NormalizeRequired(name)] = new HashSet<string>(StringComparer.OrdinalIgnoreCase), "Role"));

    public Task<StaffMetadataMutationResult> UpdateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Update(name, description, _roles, "Role"));

    public Task<StaffMetadataMutationResult> DeleteRoleAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken)
    {
        var role = Normalize(name, _roles.Keys);
        if (role is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Role '{name}' was not found."));
        }

        _roles.Remove(role);
        _rolePermissions.Remove(role);
        return Task.FromResult(StaffMetadataMutationResult.Success());
    }

    public Task<StaffMetadataMutationResult> CreateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Create(name, description, _departments, null, "Department"));

    public Task<StaffMetadataMutationResult> UpdateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Update(name, description, _departments, "Department"));

    public Task<StaffMetadataMutationResult> DeleteDepartmentAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken) =>
        Task.FromResult(Delete(name, _departments, "Department"));

    public Task<StaffMetadataMutationResult> CreatePermissionAsync(string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Create(name, description, _permissions, null, "Permission"));

    public Task<StaffMetadataMutationResult> UpdatePermissionAsync(string name, string? description, CancellationToken cancellationToken) =>
        Task.FromResult(Update(name, description, _permissions, "Permission"));

    public Task<StaffMetadataMutationResult> DeletePermissionAsync(string name, bool deleteSystem, CancellationToken cancellationToken)
    {
        var permission = Normalize(name, _permissions.Keys);
        if (permission is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Permission '{name}' was not found."));
        }

        if (_rolePermissions.Values.Any(grants => grants.Contains(permission)))
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Permission '{permission}' is granted to one or more roles."));
        }

        _permissions.Remove(permission);
        return Task.FromResult(StaffMetadataMutationResult.Success());
    }

    public Task<StaffMetadataMutationResult> GrantPermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken)
    {
        var role = Normalize(roleName, _roles.Keys);
        if (role is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Role '{roleName}' was not found."));
        }

        var permission = Normalize(permissionName, _permissions.Keys);
        if (permission is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Permission '{permissionName}' was not found."));
        }

        if (!_rolePermissions.TryGetValue(role, out var permissions))
        {
            permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _rolePermissions[role] = permissions;
        }

        return Task.FromResult(permissions.Add(permission)
            ? StaffMetadataMutationResult.Success()
            : StaffMetadataMutationResult.Failure($"Permission '{permission}' is already granted to role '{role}'."));
    }

    public Task<StaffMetadataMutationResult> RevokePermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken)
    {
        var role = Normalize(roleName, _roles.Keys);
        if (role is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Role '{roleName}' was not found."));
        }

        var permission = Normalize(permissionName, _permissions.Keys);
        if (permission is null)
        {
            return Task.FromResult(StaffMetadataMutationResult.Failure($"Permission '{permissionName}' was not found."));
        }

        return Task.FromResult(_rolePermissions.TryGetValue(role, out var permissions) && permissions.Remove(permission)
            ? StaffMetadataMutationResult.Success()
            : StaffMetadataMutationResult.Failure($"Permission '{permission}' is not granted to role '{role}'."));
    }

    private static StaffMetadataMutationResult Create(string name, string? description, Dictionary<string, string?> store, Action? afterCreate, string label)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return StaffMetadataMutationResult.Failure($"{label} name is required.");
        }

        var normalizedName = name.Trim();
        if (store.ContainsKey(normalizedName))
        {
            return StaffMetadataMutationResult.Failure($"{label} '{normalizedName}' already exists.");
        }

        store[normalizedName] = NormalizeOptional(description);
        afterCreate?.Invoke();
        return StaffMetadataMutationResult.Success();
    }

    private static StaffMetadataMutationResult Update(string name, string? description, Dictionary<string, string?> store, string label)
    {
        var normalizedName = Normalize(name, store.Keys);
        if (normalizedName is null)
        {
            return StaffMetadataMutationResult.Failure($"{label} '{name}' was not found.");
        }

        store[normalizedName] = NormalizeOptional(description);
        return StaffMetadataMutationResult.Success();
    }

    private static StaffMetadataMutationResult Delete(string name, Dictionary<string, string?> store, string label)
    {
        var normalizedName = Normalize(name, store.Keys);
        if (normalizedName is null)
        {
            return StaffMetadataMutationResult.Failure($"{label} '{name}' was not found.");
        }

        store.Remove(normalizedName);
        return StaffMetadataMutationResult.Success();
    }

    private static string? Normalize(string value, IEnumerable<string> allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return allowedValues.FirstOrDefault(allowed => string.Equals(allowed, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Metadata name is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
