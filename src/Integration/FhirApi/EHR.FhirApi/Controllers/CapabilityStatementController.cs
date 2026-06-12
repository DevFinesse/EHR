using Microsoft.AspNetCore.Mvc;

namespace EHR.FhirApi.Controllers;

[ApiController]
public sealed class CapabilityStatementController : ControllerBase
{
    [HttpGet("metadata")]
    public IActionResult Get()
    {
        return Ok(new
        {
            resourceType = "CapabilityStatement",
            status = "active",
            date = DateTimeOffset.UtcNow,
            kind = "instance",
            fhirVersion = "4.0.1",
            format = new[] { "json" },
            rest = new[]
            {
                new
                {
                    mode = "server",
                    resource = new object[]
                    {
                        Resource("Patient", ["read", "search-type"]),
                        Resource("Practitioner", ["read"]),
                        Resource("Appointment", ["read", "search-type"]),
                        Resource("Encounter", ["read", "search-type"]),
                        Resource("Observation", ["search-type"]),
                        Resource("Condition", ["search-type"])
                    }
                }
            }
        });
    }

    private static object Resource(string type, string[] interactions) => new
    {
        type,
        interaction = interactions.Select(code => new { code }).ToArray()
    };
}
