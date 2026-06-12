using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public sealed class EncounterController : ControllerBase
{
    private readonly FhirUpstreamClient _upstream;

    public EncounterController(FhirUpstreamClient upstream)
    {
        _upstream = upstream;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        var result = await _upstream.GetEncounterAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(FhirJson.Encounter(result.Value)) : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Encounter could not be read."));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] Guid? patient, [FromQuery] string? status, [FromQuery(Name = "_count")] int count = 50, CancellationToken cancellationToken = default)
    {
        var result = await _upstream.SearchEncountersAsync(ToEncounterQuery(patient, status, count), cancellationToken);
        return result.IsSuccess
            ? Ok(FhirJson.Bundle(Request, "Encounter", result.Value.Select(FhirJson.Encounter)))
            : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Encounter search failed."));
    }

    internal static string ToEncounterQuery(Guid? patient, string? status, int count) =>
        QueryString.Create(new Dictionary<string, string?>
        {
            ["patientId"] = patient?.ToString(),
            ["status"] = status,
            ["limit"] = count.ToString()
        }.Where(value => !string.IsNullOrWhiteSpace(value.Value))).ToUriComponent();
}
