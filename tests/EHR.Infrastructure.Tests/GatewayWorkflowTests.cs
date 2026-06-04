using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHR.Infrastructure.Tests;

public sealed class GatewayWorkflowTests
{
    [Fact]
    public async Task Gateway_routes_complete_clinical_workflow()
    {
        if (!IsEnabled())
        {
            return;
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri(Environment.GetEnvironmentVariable("GATEWAY_BASE_URL") ?? "http://localhost:5190")
        };

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hospital = await PostJsonAsync(client, "/tenant/api/hospitals", new
        {
            name = $"Gateway Test Clinic {suffix}",
            country = "Nigeria",
            city = "Lagos",
            plan = "Enterprise"
        });
        var tenantId = hospital.GetProperty("tenantId").GetString()!;

        var invitation = await PostJsonAsync(client, "/identity/api/staff/invitations", new
        {
            tenantId,
            fullName = "Dr Gateway Test",
            email = $"gateway-{suffix}@example.test",
            role = "Doctor",
            department = "Primary Care"
        });
        var staffUserId = invitation.GetProperty("staffUserId").GetGuid();
        var invitationToken = invitation.GetProperty("invitationToken").GetString()!;

        var token = await PostJsonAsync(client, "/identity/api/auth/invitations/accept", new
        {
            invitationToken,
            password = "GatewayTest!234",
            mfaCode = (string?)null
        });
        Assert.False(string.IsNullOrWhiteSpace(token.GetProperty("accessToken").GetString()));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());

        var patient = await PostJsonAsync(client, "/patient/api/patients", new
        {
            tenantId,
            fullName = "Amina Gateway",
            dateOfBirth = "1992-04-12",
            sex = "Female",
            phoneNumber = "+2348012345678"
        });
        var patientId = patient.GetProperty("id").GetGuid();

        var appointment = await PostJsonAsync(client, "/appointment/api/appointments", new
        {
            tenantId,
            patientId,
            practitionerId = staffUserId,
            scheduledFor = DateTimeOffset.UtcNow.AddHours(2),
            reason = "Initial consultation"
        });
        var appointmentId = appointment.GetProperty("id").GetGuid();

        var checkedIn = await PostJsonAsync(client, $"/appointment/api/appointments/{appointmentId}/check-in", new { });
        Assert.Equal("CheckedIn", checkedIn.GetProperty("status").GetString());

        var encounter = await PostJsonAsync(client, "/encounter/api/encounters", new
        {
            tenantId,
            appointmentId,
            patientId,
            practitionerId = staffUserId,
            visitType = "Clinic"
        });
        var encounterId = encounter.GetProperty("id").GetGuid();

        await PostJsonAsync(client, $"/encounter/api/encounters/{encounterId}/vitals", new
        {
            temperatureCelsius = 36.8m,
            systolicBloodPressure = 118,
            diastolicBloodPressure = 76,
            pulseRate = 72,
            oxygenSaturation = 98
        });

        await PostJsonAsync(client, $"/encounter/api/encounters/{encounterId}/diagnoses", new
        {
            code = "Z00.0",
            description = "General medical examination",
            certainty = "Confirmed"
        });

        var completed = await PostJsonAsync(client, $"/encounter/api/encounters/{encounterId}/complete", new { });
        Assert.Equal("Completed", completed.GetProperty("status").GetString());
    }

    private static bool IsEnabled() =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_E2E_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    private static async Task<JsonElement> PostJsonAsync(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body);
        var json = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
