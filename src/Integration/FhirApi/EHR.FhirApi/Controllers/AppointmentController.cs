using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public sealed class AppointmentController : ControllerBase
{
    private readonly FhirUpstreamClient _upstream;

    public AppointmentController(FhirUpstreamClient upstream)
    {
        _upstream = upstream;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        var result = await _upstream.GetAppointmentAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(FhirJson.Appointment(result.Value)) : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Appointment could not be read."));
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] DateTimeOffset? date, [FromQuery] Guid? practitioner, [FromQuery] string? status, [FromQuery(Name = "_count")] int count = 50, CancellationToken cancellationToken = default)
    {
        var query = QueryString.Create(BuildQuery([
            ("from", date?.ToString("O")),
            ("to", date?.Date.AddDays(1).ToString("O")),
            ("practitionerId", practitioner?.ToString()),
            ("status", status),
            ("limit", count.ToString())
        ])).ToUriComponent();

        var result = await _upstream.SearchAppointmentsAsync(query, cancellationToken);
        return result.IsSuccess
            ? Ok(FhirJson.Bundle(Request, "Appointment", result.Value.Select(FhirJson.Appointment)))
            : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Appointment search failed."));
    }

    private static IEnumerable<KeyValuePair<string, string?>> BuildQuery(IEnumerable<(string Key, string? Value)> values) =>
        values.Where(value => !string.IsNullOrWhiteSpace(value.Value)).Select(value => new KeyValuePair<string, string?>(value.Key, value.Value));
}
