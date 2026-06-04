using Microsoft.AspNetCore.Mvc;

namespace EHR.TenantService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.TenantService", owns = "Hospitals, branches, departments, subscriptions" });
}
