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

    [HttpPut("{id:guid}/demographics")]
    [Authorize(Policy = PlatformPermissions.PatientsUpdate)]
    public async Task<IActionResult> UpdateDemographics(Guid id, UpdatePatientDemographicsRequest request, CancellationToken cancellationToken)
    {
        var patient = await _cqrs.SendAsync(new UpdatePatientDemographicsCommand(id, request.FullName, request.DateOfBirth, request.Sex, request.PhoneNumber), cancellationToken);
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

public sealed record UpdatePatientDemographicsRequest(string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber);
