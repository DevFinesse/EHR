using EHR.AnalyticsService.Omop;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.AnalyticsService.Controllers;

[ApiController]
[Route("api/omop")]
[Authorize(Policy = PlatformPermissions.AnalyticsRead)]
public sealed class OmopController : ControllerBase
{
    private readonly OmopQueryService _queries;

    public OmopController(OmopQueryService queries)
    {
        _queries = queries;
    }

    [HttpGet("persons")]
    public async Task<IActionResult> Persons([FromQuery] string? tenantId, [FromQuery] int limit = 100, CancellationToken cancellationToken = default) =>
        Ok(await _queries.PersonsAsync(tenantId, limit, cancellationToken));

    [HttpGet("visit-occurrences")]
    public async Task<IActionResult> Visits([FromQuery] string? tenantId, [FromQuery] Guid? personId, [FromQuery] int limit = 100, CancellationToken cancellationToken = default) =>
        Ok(await _queries.VisitsAsync(tenantId, personId, limit, cancellationToken));

    [HttpGet("condition-occurrences")]
    public async Task<IActionResult> Conditions([FromQuery] string? tenantId, [FromQuery] Guid? personId, [FromQuery] int limit = 100, CancellationToken cancellationToken = default) =>
        Ok(await _queries.ConditionsAsync(tenantId, personId, limit, cancellationToken));

    [HttpGet("measurements")]
    public async Task<IActionResult> Measurements([FromQuery] string? tenantId, [FromQuery] Guid? personId, [FromQuery] string? sourceValue, [FromQuery] int limit = 100, CancellationToken cancellationToken = default) =>
        Ok(await _queries.MeasurementsAsync(tenantId, personId, sourceValue, limit, cancellationToken));
}
