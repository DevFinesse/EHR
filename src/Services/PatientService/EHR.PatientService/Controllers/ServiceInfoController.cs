using Microsoft.AspNetCore.Mvc;

namespace EHR.PatientService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.PatientService", owns = "Patient demographics, identifiers, contacts, insurance profile" });
}
