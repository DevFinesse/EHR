using EHR.AppointmentService.Application.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.AppointmentService.Controllers;

[ApiController]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public AppointmentsController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.AppointmentsBook)]
    public async Task<IActionResult> Book(BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Policy = PlatformPermissions.AppointmentsCheckIn)]
    public async Task<IActionResult> CheckIn(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(new CheckInPatientCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.AppointmentsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _cqrs.QueryAsync(new GetAppointmentByIdQuery(id), cancellationToken);
        return appointment is null ? NotFound() : Ok(appointment);
    }
}
