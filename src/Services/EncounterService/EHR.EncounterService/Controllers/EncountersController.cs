using EHR.Cqrs;
using EHR.EncounterService.Application.Encounters;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.EncounterService.Controllers;

[ApiController]
[Route("api/encounters")]
public sealed class EncountersController : ControllerBase
{
    private readonly ICqrsDispatcher _cqrs;

    public EncountersController(ICqrsDispatcher cqrs)
    {
        _cqrs = cqrs;
    }

    [HttpPost]
    [Authorize(Policy = PlatformPermissions.EncountersWrite)]
    public async Task<IActionResult> Start(StartEncounterCommand command, CancellationToken cancellationToken)
    {
        var encounter = await _cqrs.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = encounter.Id }, encounter);
    }

    [HttpPost("{id:guid}/vitals")]
    [Authorize(Policy = PlatformPermissions.EncountersWrite)]
    public async Task<IActionResult> RecordVitals(Guid id, RecordVitalsRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordVitalsCommand(id, request.TemperatureCelsius, request.SystolicBloodPressure, request.DiastolicBloodPressure, request.PulseRate, request.OxygenSaturation);
        var result = await _cqrs.SendAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/diagnoses")]
    [Authorize(Policy = PlatformPermissions.EncountersWrite)]
    public async Task<IActionResult> AddDiagnosis(Guid id, AddDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(new AddDiagnosisCommand(id, request.Code, request.Description, request.Certainty), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = PlatformPermissions.EncountersWrite)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _cqrs.SendAsync(new CompleteEncounterCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PlatformPermissions.EncountersRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var encounter = await _cqrs.QueryAsync(new GetEncounterByIdQuery(id), cancellationToken);
        return encounter is null ? NotFound() : Ok(encounter);
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.EncountersRead)]
    public async Task<IActionResult> List(
        [FromQuery] string? tenantId,
        [FromQuery] Guid? patientId,
        [FromQuery] string? status,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var encounters = await _cqrs.QueryAsync(new ListEncountersQuery(tenantId, patientId, status, limit), cancellationToken);
        return Ok(encounters);
    }
}
