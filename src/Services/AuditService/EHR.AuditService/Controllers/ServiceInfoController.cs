using Microsoft.AspNetCore.Mvc;

namespace EHR.AuditService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.AuditService", owns = "Immutable audit records and compliance event history" });
}
