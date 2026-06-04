using Microsoft.AspNetCore.Mvc;

namespace EHR.EncounterService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.EncounterService", owns = "Visits, vitals, observations, diagnosis, treatment plans" });
}
