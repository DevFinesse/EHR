using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public sealed class PractitionerController : ControllerBase
{
    private readonly FhirUpstreamClient _upstream;

    public PractitionerController(FhirUpstreamClient upstream)
    {
        _upstream = upstream;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        var result = await _upstream.GetPractitionerAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(FhirJson.Practitioner(result.Value)) : StatusCode((int)result.StatusCode, FhirJson.OperationOutcome(result.Error ?? "Practitioner could not be read."));
    }
}
