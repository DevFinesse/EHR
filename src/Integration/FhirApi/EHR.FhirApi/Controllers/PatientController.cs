using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public sealed class PatientController : ControllerBase
{
    private readonly FhirUpstreamClient _upstream;

    public PatientController(FhirUpstreamClient upstream)
    {
        _upstream = upstream;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        var result = await _upstream.GetPatientAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(FhirJson.Patient(result.Value)) : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Patient could not be read."));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? identifier, [FromQuery] string? name, [FromQuery] string? phone, [FromQuery(Name = "_count")] int count = 50, CancellationToken cancellationToken = default)
    {
        var query = QueryString.Create(BuildQuery([
            ("medicalRecordNumber", identifier),
            ("name", name),
            ("phoneNumber", phone),
            ("limit", count.ToString())
        ])).ToUriComponent();

        var result = await _upstream.SearchPatientsAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(FhirJson.Bundle(Request, "Patient", result.Value.Select(FhirJson.Patient)))
            : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Patient search failed."));
    }

    private static IEnumerable<KeyValuePair<string, string?>> BuildQuery(IEnumerable<(string Key, string? Value)> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value.Value)).Select(value => new KeyValuePair<string, string?>(value.Key, value.Value));
}
