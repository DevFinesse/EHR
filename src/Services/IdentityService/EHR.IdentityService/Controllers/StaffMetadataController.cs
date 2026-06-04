using EHR.IdentityService.Domain.Staff;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.IdentityService.Controllers;

[ApiController]
[Route("api/staff-metadata")]
[Authorize(Policy = PlatformPermissions.StaffMetadataRead)]
public sealed class StaffMetadataController : ControllerBase
{
    [HttpGet("roles")]
    public IActionResult GetRoles() => Ok(PlatformRoles.All);

    [HttpGet("departments")]
    public IActionResult GetDepartments() => Ok(StaffDepartments.All);

    [HttpGet("permissions")]
    public IActionResult GetPermissions() => Ok(PlatformPermissions.All);

    [HttpGet("role-permissions")]
    public IActionResult GetRolePermissions() => Ok(RolePermissionMap.All);
}
