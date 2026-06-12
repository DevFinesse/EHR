using EHR.AnalyticsService.Omop;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.AnalyticsService.Controllers;

[ApiController]
[Route("api/omop/concept-maps")]
public sealed class ConceptMapsController : ControllerBase
{
    private readonly OmopConceptMapService _conceptMaps;

    public ConceptMapsController(OmopConceptMapService conceptMaps)
    {
        _conceptMaps = conceptMaps;
    }

    [HttpGet]
    [Authorize(Policy = PlatformPermissions.AnalyticsRead)]
    public async Task<IActionResult> List(
        [FromQuery] string? domain,
        [FromQuery] string? sourceVocabulary,
        [FromQuery] string? sourceCode,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _conceptMaps.ListAsync(domain, sourceVocabulary, sourceCode, limit, cancellationToken));
    }

    [HttpPut]
    [Authorize(Policy = PlatformPermissions.AnalyticsManage)]
    public async Task<IActionResult> Upsert(UpsertConceptMapRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Domain) ||
            string.IsNullOrWhiteSpace(request.SourceVocabulary) ||
            string.IsNullOrWhiteSpace(request.SourceCode) ||
            string.IsNullOrWhiteSpace(request.StandardVocabulary) ||
            string.IsNullOrWhiteSpace(request.StandardConceptCode) ||
            string.IsNullOrWhiteSpace(request.StandardConceptName) ||
            request.StandardConceptId <= 0)
        {
            return BadRequest(new { error = "Domain, source vocabulary/code, standard vocabulary/code/name, and a positive standard concept id are required." });
        }

        return Ok(await _conceptMaps.UpsertAsync(request, cancellationToken));
    }
}
