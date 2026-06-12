using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public sealed class ObservationController : ControllerBase
{
    private readonly FhirUpstreamClient _upstream;

    public ObservationController(FhirUpstreamClient upstream)
    {
        _upstream = upstream;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? patient, [FromQuery] Guid? encounter, [FromQuery(Name = "_count")] int count = 50, CancellationToken cancellationToken = default)
    {
        var encounters = await GetEncountersAsync(patient, encounter, count, cancellationToken);
        return encounters.IsSuccess
            ? Ok(FhirJson.Bundle(Request, "Observation", encounters.Value.SelectMany(FhirJson.Observations)))
            : StatusCode((int)encounters.StatusCode, FhirJson.OperationOutcome(encounters.Error ?? "Observation search failed."));
    }

    private async Task<UpstreamResult<IReadOnlyCollection<System.Text.Json.JsonElement>>> GetEncountersAsync(Guid? patient, Guid? encounter, int count, CancellationToken cancellationToken)
    {
        if (encounter is not null)
        {
            var result = await _upstream.GetEncounterAsync(encounter.Value, cancellationToken);
            return result.IsSuccess
                ? UpstreamResult<IReadOnlyCollection<System.Text.Json.JsonElement>>.Success([result.Value])
                : UpstreamResult<IReadOnlyCollection<System.Text.Json.JsonElement>>.Failure(result.StatusCode, result.Error);
        }

        return await _upstream.SearchEncountersAsync(EncounterController.ToEncounterQuery(patient, null, count), cancellationToken);
    }
}
