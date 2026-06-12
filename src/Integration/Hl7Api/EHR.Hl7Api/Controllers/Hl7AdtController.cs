using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EHR.Hl7Api.Controllers;

[ApiController]
[Authorize]
[Route("api/hl7/adt")]
public sealed class Hl7AdtController : ControllerBase
{
    private readonly Hl7MessageParser _parser;
    private readonly Hl7AdtMessageBuilder _builder;
    private readonly Hl7AdtWorkflowService _workflow;

    public Hl7AdtController(Hl7MessageParser parser, Hl7AdtMessageBuilder builder, Hl7AdtWorkflowService workflow)
    {
        _parser = parser;
        _builder = builder;
        _workflow = workflow;
    }

    [HttpGet("capabilities")]
    [AllowAnonymous]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            standard = "HL7 v2",
            version = "2.5.1",
            messageType = "ADT",
            triggerEvents = new[] { "A01", "A04", "A08" },
            inbound = new
            {
                endpoint = "/api/hl7/adt/inbound",
                contentTypes = new[] { "text/plain", "application/hl7-v2" },
                acknowledgment = "ACK with MSA AA on success, AE on parse or validation failure"
            },
            outbound = new
            {
                endpoint = "/api/hl7/adt/outbound",
                contentType = "application/json",
                produces = "application/hl7-v2"
            }
        });
    }

    [HttpPost("inbound")]
    [Consumes("text/plain", "application/hl7-v2")]
    [Produces("application/hl7-v2")]
    public async Task<IActionResult> Receive([FromBody] string hl7Message, [FromQuery] string? tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = _parser.Parse(hl7Message);
            var result = await _workflow.ApplyAsync(parsed, tenantId, cancellationToken);
            Response.Headers["X-EHR-HL7-Action"] = result.Action;
            Response.Headers["X-EHR-Patient-Id"] = result.PatientId.ToString();
            Response.Headers["X-EHR-Medical-Record-Number"] = result.MedicalRecordNumber;
            return Content(_builder.BuildAck(parsed, text: $"ADT {parsed.TriggerEvent} {result.Action} patient {result.PatientId}"), "application/hl7-v2");
        }
        catch (Exception exception) when (exception is Hl7ParseException or Hl7WorkflowException)
        {
            var controlId = TryReadControlId(hl7Message);
            return StatusCode(StatusCodes.Status422UnprocessableEntity, _builder.BuildApplicationErrorAck(controlId, exception.Message));
        }
    }

    [HttpPost("parse")]
    [Consumes("text/plain", "application/hl7-v2")]
    public IActionResult Parse([FromBody] string hl7Message)
    {
        try
        {
            return Ok(_parser.Parse(hl7Message));
        }
        catch (Hl7ParseException exception)
        {
            return UnprocessableEntity(new { error = exception.Message });
        }
    }

    [HttpPost("outbound")]
    [Produces("application/hl7-v2")]
    public IActionResult Build(BuildAdtMessageRequest request)
    {
        try
        {
            return Content(_builder.Build(request), "application/hl7-v2");
        }
        catch (Hl7ParseException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("outbound/json")]
    public IActionResult BuildJson(BuildAdtMessageRequest request)
    {
        try
        {
            var trigger = request.TriggerEvent.Trim().ToUpperInvariant();
            return Ok(new BuildAdtMessageResponse("ADT", trigger, request.MessageControlId, _builder.Build(request)));
        }
        catch (Hl7ParseException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private static string TryReadControlId(string? hl7Message)
    {
        if (string.IsNullOrWhiteSpace(hl7Message))
        {
            return string.Empty;
        }

        var msh = hl7Message.Replace("\r\n", "\r", StringComparison.Ordinal).Replace('\n', '\r')
            .Split('\r', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(segment => segment.StartsWith("MSH|", StringComparison.OrdinalIgnoreCase));

        var fields = msh?.Split('|');
        return fields is { Length: > 9 } ? fields[9] : string.Empty;
    }
}
