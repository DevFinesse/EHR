using System.Text.Json;

namespace EHR.FhirApi;

public static class FhirJson
{
    public static object Bundle(HttpRequest request, string resourceType, IEnumerable<object> resources)
    {
        var entries = resources
            .Select(resource => new
            {
                fullUrl = $"{request.Scheme}://{request.Host}{request.PathBase}/{resourceType}/{GetId(resource)}",
                resource
            })
            .ToArray();

        return new
        {
            resourceType = "Bundle",
            type = "searchset",
            total = entries.Length,
            entry = entries
        };
    }

    public static object OperationOutcome(string diagnostics) => new
    {
        resourceType = "OperationOutcome",
        issue = new[]
        {
            new
            {
                severity = "error",
                code = "processing",
                diagnostics
            }
        }
    };

    public static object Patient(JsonElement source) => new
    {
        resourceType = "Patient",
        id = Text(source, "id"),
        identifier = new[]
        {
            new
            {
                system = "urn:ehr:mrn",
                value = Text(source, "medicalRecordNumber")
            }
        },
        name = new[]
        {
            new
            {
                use = "official",
                text = Text(source, "fullName")
            }
        },
        telecom = new[]
        {
            new
            {
                system = "phone",
                value = Text(source, "phoneNumber"),
                use = "mobile"
            }
        },
        gender = Gender(Text(source, "sex")),
        birthDate = Text(source, "dateOfBirth"),
        meta = Meta(Text(source, "tenantId"))
    };

    public static object Practitioner(JsonElement source) => new
    {
        resourceType = "Practitioner",
        id = Text(source, "id"),
        identifier = new[]
        {
            new
            {
                system = "urn:ehr:staff",
                value = Text(source, "id")
            }
        },
        name = new[]
        {
            new
            {
                use = "official",
                text = Text(source, "fullName")
            }
        },
        telecom = new[]
        {
            new
            {
                system = "email",
                value = Text(source, "email"),
                use = "work"
            }
        },
        qualification = new[]
        {
            new
            {
                code = Coding("urn:ehr:staff-role", Text(source, "role")),
                issuer = new { display = Text(source, "department") }
            }
        },
        meta = Meta(Text(source, "tenantId"))
    };

    public static object Appointment(JsonElement source) => new
    {
        resourceType = "Appointment",
        id = Text(source, "id"),
        status = AppointmentStatus(Text(source, "status")),
        start = Text(source, "scheduledFor"),
        reasonCode = new[]
        {
            new { text = Text(source, "reason") }
        },
        participant = new object[]
        {
            new
            {
                actor = Reference("Patient", Text(source, "patientId")),
                status = "accepted"
            },
            new
            {
                actor = Reference("Practitioner", Text(source, "practitionerId")),
                status = "accepted"
            }
        },
        meta = Meta(Text(source, "tenantId"))
    };

    public static object Encounter(JsonElement source) => new Dictionary<string, object?>
    {
        ["resourceType"] = "Encounter",
        ["id"] = Text(source, "id"),
        ["status"] = EncounterStatus(Text(source, "status")),
        ["class"] = Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", Text(source, "visitType")),
        ["appointment"] = new[] { Reference("Appointment", Text(source, "appointmentId")) },
        ["subject"] = Reference("Patient", Text(source, "patientId")),
        ["participant"] = new[]
        {
            new
            {
                individual = Reference("Practitioner", Text(source, "practitionerId"))
            }
        },
        ["meta"] = Meta(Text(source, "tenantId"))
    };

    public static IEnumerable<object> Observations(JsonElement encounter)
    {
        var encounterId = Text(encounter, "id");
        var patientId = Text(encounter, "patientId");
        foreach (var vitals in Array(encounter, "vitals"))
        {
            yield return Observation(encounterId, patientId, "temperature-celsius", "Body temperature", "Cel", Decimal(vitals, "temperatureCelsius"));
            yield return Observation(encounterId, patientId, "systolic-blood-pressure", "Systolic blood pressure", "mm[Hg]", Decimal(vitals, "systolicBloodPressure"));
            yield return Observation(encounterId, patientId, "diastolic-blood-pressure", "Diastolic blood pressure", "mm[Hg]", Decimal(vitals, "diastolicBloodPressure"));
            yield return Observation(encounterId, patientId, "pulse-rate", "Pulse rate", "/min", Decimal(vitals, "pulseRate"));
            yield return Observation(encounterId, patientId, "oxygen-saturation", "Oxygen saturation", "%", Decimal(vitals, "oxygenSaturation"));
        }
    }

    public static IEnumerable<object> Conditions(JsonElement encounter)
    {
        var encounterId = Text(encounter, "id");
        var patientId = Text(encounter, "patientId");
        foreach (var diagnosis in Array(encounter, "diagnoses"))
        {
            var code = Text(diagnosis, "code");
            yield return new
            {
                resourceType = "Condition",
                id = $"{encounterId}-{code}".TrimEnd('-'),
                clinicalStatus = Coding("http://terminology.hl7.org/CodeSystem/condition-clinical", "active"),
                verificationStatus = Coding("urn:ehr:diagnosis-certainty", Text(diagnosis, "certainty")),
                code = new
                {
                    coding = new[] { Coding("urn:ehr:diagnosis-code", code) },
                    text = Text(diagnosis, "description")
                },
                subject = Reference("Patient", patientId),
                encounter = Reference("Encounter", encounterId)
            };
        }
    }

    private static object Observation(string encounterId, string patientId, string code, string display, string unit, decimal value) => new
    {
        resourceType = "Observation",
        id = $"{encounterId}-{code}",
        status = "final",
        code = new
        {
            coding = new[] { Coding("urn:ehr:vital-sign", code, display) },
            text = display
        },
        subject = Reference("Patient", patientId),
        encounter = Reference("Encounter", encounterId),
        valueQuantity = new
        {
            value,
            unit,
            system = "http://unitsofmeasure.org",
            code = unit
        }
    };

    private static object Reference(string resourceType, string id) => new
    {
        reference = $"{resourceType}/{id}"
    };

    private static object Coding(string system, string code, string? display = null) => new
    {
        system,
        code,
        display = display ?? code
    };

    private static object Meta(string tenantId) => new
    {
        tag = new[]
        {
            Coding("urn:ehr:tenant", tenantId)
        }
    };

    private static string Text(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Number => property.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static decimal Decimal(JsonElement source, string propertyName) =>
        source.TryGetProperty(propertyName, out var property) && property.TryGetDecimal(out var value) ? value : 0;

    private static IEnumerable<JsonElement> Array(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in property.EnumerateArray())
        {
            yield return item;
        }
    }

    private static string Gender(string sex) => sex.Trim().ToLowerInvariant() switch
    {
        "m" or "male" => "male",
        "f" or "female" => "female",
        _ => "unknown"
    };

    private static string AppointmentStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "booked" => "booked",
        "checkedin" => "arrived",
        _ => "proposed"
    };

    private static string EncounterStatus(string status) => status.Trim().ToLowerInvariant() switch
    {
        "completed" => "finished",
        "started" => "in-progress",
        _ => "planned"
    };

    private static string GetId(object resource)
    {
        var property = resource.GetType().GetProperty("id");
        return property?.GetValue(resource)?.ToString() ?? string.Empty;
    }
}
