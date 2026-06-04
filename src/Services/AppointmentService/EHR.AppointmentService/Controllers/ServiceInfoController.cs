using Microsoft.AspNetCore.Mvc;

namespace EHR.AppointmentService.Controllers;

[ApiController]
[Route("")]
public sealed class ServiceInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { service = "EHR.AppointmentService", owns = "Schedules, bookings, queues, check-in" });
}
