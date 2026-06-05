namespace EHR.IdentityService.Application.Staff;

public sealed record StaffRoleDefinition(string Name, IReadOnlyCollection<string> Permissions);

public sealed record StaffMetadataMutationResult(bool IsSuccess, string? Error = null)
{
    public static StaffMetadataMutationResult Success() => new(true);

    public static StaffMetadataMutationResult Failure(string error) => new(false, error);
}

public interface IStaffMetadataRepository
{
    Task<IReadOnlyCollection<string>> GetRolesAsync(string? tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetDepartmentsAsync(string? tenantId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetPermissionsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetRolePermissionsAsync(string? tenantId, CancellationToken cancellationToken);

    Task<string?> NormalizeRoleAsync(string? tenantId, string role, CancellationToken cancellationToken);

    Task<string?> NormalizeDepartmentAsync(string? tenantId, string department, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetPermissionsForRoleAsync(string? tenantId, string role, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> CreateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> UpdateRoleAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> DeleteRoleAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> CreateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> UpdateDepartmentAsync(string? tenantId, string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> DeleteDepartmentAsync(string? tenantId, string name, bool deleteSystem, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> CreatePermissionAsync(string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> UpdatePermissionAsync(string name, string? description, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> DeletePermissionAsync(string name, bool deleteSystem, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> GrantPermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken);

    Task<StaffMetadataMutationResult> RevokePermissionAsync(string? tenantId, string roleName, string permissionName, CancellationToken cancellationToken);
}
