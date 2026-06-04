using Microsoft.AspNetCore.Mvc;

namespace EHR.IdentityService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.IdentityService", owns = "Users, roles, permissions, refresh tokens" });
}
