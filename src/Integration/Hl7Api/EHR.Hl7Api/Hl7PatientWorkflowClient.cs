using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EHR.Hl7Api;

public sealed class Hl7PatientWorkflowClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Hl7PatientWorkflowClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PatientApiModel?> FindByMedicalRecordNumberAsync(string tenantId, string medicalRecordNumber, CancellationToken cancellationToken)
    {
        var query = QueryString.Create(new Dictionary<string, string?>
        {
            ["tenantId"] = tenantId,
            ["medicalRecordNumber"] = medicalRecordNumber,
            ["limit"] = "1"
        }).ToUriComponent();

        var patients = await SendAsync<IReadOnlyCollection<PatientApiModel>>(HttpMethod.Get, $"/api/patients{query}", null, cancellationToken);
        return patients.Value?.FirstOrDefault();
    }

    public async Task<PatientApiModel> RegisterAsync(string tenantId, Hl7Patient patient, CancellationToken cancellationToken)
    {
        var request = new PatientRegistrationRequest(
            tenantId,
            patient.FullName(),
            patient.DateOfBirth ?? throw new Hl7WorkflowException("PID-7 date of birth is required for patient registration."),
            patient.Sex.OrDefault("U"),
            patient.PhoneNumber.OrDefault("unknown"),
            patient.MedicalRecordNumber);

        var result = await SendAsync<PatientApiModel>(HttpMethod.Post, "/api/patients", request, cancellationToken);
        return result.Value ?? throw new Hl7WorkflowException("Patient registration did not return a patient.");
    }

    public async Task<PatientApiModel> UpdateDemographicsAsync(Guid patientId, Hl7Patient patient, CancellationToken cancellationToken)
    {
        var request = new PatientDemographicsUpdateRequest(
            patient.FullName(),
            patient.DateOfBirth ?? throw new Hl7WorkflowException("PID-7 date of birth is required for demographic updates."),
            patient.Sex.OrDefault("U"),
            patient.PhoneNumber.OrDefault("unknown"));

        var result = await SendAsync<PatientApiModel>(HttpMethod.Put, $"/api/patients/{patientId}/demographics", request, cancellationToken);
        return result.Value ?? throw new Hl7WorkflowException("Patient demographic update did not return a patient.");
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string pathAndQuery, object? body, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration["ServiceClients:PatientBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new Hl7WorkflowException("ServiceClients:PatientBaseUrl is not configured.");
        }

        using var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), pathAndQuery));
        ForwardBearerToken(request);

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Hl7WorkflowException($"Patient service returned {(int)response.StatusCode} {response.StatusCode}: {error}");
        }

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return new ApiResult<T>(default);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return new ApiResult<T>(await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken));
    }

    private void ForwardBearerToken(HttpRequestMessage request)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        }
    }

    private sealed record ApiResult<T>(T? Value);
}

public sealed class PatientApiModel
{
    public Guid Id { get; set; }
    public object? TenantId { get; set; }
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed record PatientRegistrationRequest(string TenantId, string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber, string? MedicalRecordNumber);

public sealed record PatientDemographicsUpdateRequest(string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber);

public sealed class Hl7WorkflowException : Exception
{
    public Hl7WorkflowException(string message) : base(message)
    {
    }
}

internal static class Hl7PatientExtensions
{
    public static string FullName(this Hl7Patient patient)
    {
        var fullName = string.Join(' ', new[] { patient.GivenName, patient.FamilyName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? "Unknown Patient" : fullName;
    }

    public static string OrDefault(this string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
