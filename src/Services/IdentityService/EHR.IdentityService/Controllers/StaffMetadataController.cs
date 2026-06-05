using EHR.IdentityService.Application.Staff;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.IdentityService.Controllers;

[ApiController]
[Route("api/staff-metadata")]
public sealed class StaffMetadataController : ControllerBase
{
    private readonly IStaffMetadataRepository _staffMetadata;
    private readonly ICurrentUserContext _currentUser;

    public StaffMetadataController(IStaffMetadataRepository staffMetadata, ICurrentUserContext currentUser)
    {
        _staffMetadata = staffMetadata;
        _currentUser = currentUser;
    }

    [HttpGet("roles")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataRead)]
    public async Task<IActionResult> GetRoles([FromQuery] bool global = false, CancellationToken cancellationToken = default) => Ok(await _staffMetadata.GetRolesAsync(ResolveScope(global), cancellationToken));

    [HttpPost("roles")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> CreateRole(StaffMetadataItemRequest request, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.CreateRoleAsync(ResolveScope(global), request.Name, request.Description, cancellationToken);
        return result.IsSuccess ? Created($"/api/staff-metadata/roles/{Uri.EscapeDataString(request.Name.Trim())}", null) : ToError(result);
    }

    [HttpPut("roles/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> UpdateRole(string name, StaffMetadataDescriptionRequest request, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.UpdateRoleAsync(ResolveScope(global), name, request.Description, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpDelete("roles/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> DeleteRole(string name, [FromQuery] bool forceSystem = false, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.DeleteRoleAsync(ResolveScope(global), name, forceSystem, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpGet("departments")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataRead)]
    public async Task<IActionResult> GetDepartments([FromQuery] bool global = false, CancellationToken cancellationToken = default) => Ok(await _staffMetadata.GetDepartmentsAsync(ResolveScope(global), cancellationToken));

    [HttpPost("departments")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> CreateDepartment(StaffMetadataItemRequest request, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.CreateDepartmentAsync(ResolveScope(global), request.Name, request.Description, cancellationToken);
        return result.IsSuccess ? Created($"/api/staff-metadata/departments/{Uri.EscapeDataString(request.Name.Trim())}", null) : ToError(result);
    }

    [HttpPut("departments/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> UpdateDepartment(string name, StaffMetadataDescriptionRequest request, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.UpdateDepartmentAsync(ResolveScope(global), name, request.Description, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpDelete("departments/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> DeleteDepartment(string name, [FromQuery] bool forceSystem = false, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.DeleteDepartmentAsync(ResolveScope(global), name, forceSystem, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpGet("permissions")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken) => Ok(await _staffMetadata.GetPermissionsAsync(cancellationToken));

    [HttpPost("permissions")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> CreatePermission(StaffMetadataItemRequest request, CancellationToken cancellationToken)
    {
        var result = await _staffMetadata.CreatePermissionAsync(request.Name, request.Description, cancellationToken);
        return result.IsSuccess ? Created($"/api/staff-metadata/permissions/{Uri.EscapeDataString(request.Name.Trim())}", null) : ToError(result);
    }

    [HttpPut("permissions/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> UpdatePermission(string name, StaffMetadataDescriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _staffMetadata.UpdatePermissionAsync(name, request.Description, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpDelete("permissions/{name}")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> DeletePermission(string name, [FromQuery] bool forceSystem = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.DeletePermissionAsync(name, forceSystem, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    [HttpGet("role-permissions")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataRead)]
    public async Task<IActionResult> GetRolePermissions([FromQuery] bool global = false, CancellationToken cancellationToken = default) => Ok(await _staffMetadata.GetRolePermissionsAsync(ResolveScope(global), cancellationToken));

    [HttpPost("role-permissions")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> GrantRolePermission(StaffRolePermissionRequest request, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.GrantPermissionAsync(ResolveScope(global), request.RoleName, request.PermissionName, cancellationToken);
        return result.IsSuccess ? Created("/api/staff-metadata/role-permissions", null) : ToError(result);
    }

    [HttpDelete("role-permissions")]
    [Authorize(Policy = PlatformPermissions.StaffMetadataManage)]
    public async Task<IActionResult> RevokeRolePermission([FromQuery] string roleName, [FromQuery] string permissionName, [FromQuery] bool global = false, CancellationToken cancellationToken = default)
    {
        var result = await _staffMetadata.RevokePermissionAsync(ResolveScope(global), roleName, permissionName, cancellationToken);
        return result.IsSuccess ? NoContent() : ToError(result);
    }

    private IActionResult ToError(StaffMetadataMutationResult result)
    {
        var error = result.Error ?? "Metadata operation failed.";
        if (error.Contains("was not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { error });
        }

        if (error.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { error });
        }

        return BadRequest(new { error });
    }

    private string? ResolveScope(bool global)
    {
        if (global)
        {
            if (!_currentUser.IsSuperAdmin)
            {
                throw new UnauthorizedAccessException("Only Super Admin users can manage global staff metadata.");
            }

            return null;
        }

        if (_currentUser.IsSuperAdmin)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(_currentUser.TenantId)
            ? throw new UnauthorizedAccessException("Tenant-scoped staff metadata requires a tenant_id claim.")
            : _currentUser.TenantId;
    }
}

public sealed record StaffMetadataItemRequest(string Name, string? Description);

public sealed record StaffMetadataDescriptionRequest(string? Description);

public sealed record StaffRolePermissionRequest(string RoleName, string PermissionName);
