using EHR.Cqrs;
using EHR.PatientService.Application.Patients;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.PatientService.Controllers;

[ApiController]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public PatientsController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.PatientsCreate)]
    public async Task<IActionResult> Register(RegisterPatientCommand command, CancellationToken cancellationToken)
    {
        var patient = await _cqrs.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.PatientsRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _cqrs.QueryAsync(new GetPatientByIdQuery(id), cancellationToken);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.PatientsRead)]
    public async Task<IActionResult> Search(
        [FromQuery] string? tenantId,
        [FromQuery] string? medicalRecordNumber,
        [FromQuery] string? name,
        [FromQuery] string? phoneNumber,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var patients = await _cqrs.QueryAsync(new SearchPatientsQuery(tenantId, medicalRecordNumber, name, phoneNumber, limit), cancellationToken);
        return Ok(patients);
    }
}
