using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EHR.FhirApi;

public sealed class FhirUpstreamClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FhirUpstreamClient(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<UpstreamResult<JsonElement>> GetPatientAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync("PatientBaseUrl", $"/api/patients/{id}", cancellationToken);

    public Task<UpstreamResult<IReadOnlyCollection<JsonElement>>> SearchPatientsAsync(string query, CancellationToken cancellationToken) =>
        GetListAsync("PatientBaseUrl", $"/api/patients{query}", cancellationToken);

    public Task<UpstreamResult<JsonElement>> GetPractitionerAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync("IdentityBaseUrl", $"/api/staff/{id}", cancellationToken);

    public Task<UpstreamResult<JsonElement>> GetAppointmentAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync("AppointmentBaseUrl", $"/api/appointments/{id}", cancellationToken);

    public Task<UpstreamResult<IReadOnlyCollection<JsonElement>>> SearchAppointmentsAsync(string query, CancellationToken cancellationToken) =>
        GetListAsync("AppointmentBaseUrl", $"/api/appointments{query}", cancellationToken);

    public Task<UpstreamResult<JsonElement>> GetEncounterAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync("EncounterBaseUrl", $"/api/encounters/{id}", cancellationToken);

    public Task<UpstreamResult<IReadOnlyCollection<JsonElement>>> SearchEncountersAsync(string query, CancellationToken cancellationToken) =>
        GetListAsync("EncounterBaseUrl", $"/api/encounters{query}", cancellationToken);

    private async Task<UpstreamResult<IReadOnlyCollection<JsonElement>>> GetListAsync(string baseUrlKey, string pathAndQuery, CancellationToken cancellationToken)
    {
        var result = await GetAsync(baseUrlKey, pathAndQuery, cancellationToken);
        if (!result.IsSuccess)
        {
            return UpstreamResult<IReadOnlyCollection<JsonElement>>.Failure(result.StatusCode, result.Error);
        }

        var items = new List<JsonElement>();
        foreach (var item in result.Value.EnumerateArray())
        {
            items.Add(item.Clone());
        }

        return UpstreamResult<IReadOnlyCollection<JsonElement>>.Success(items);
    }

    private async Task<UpstreamResult<JsonElement>> GetAsync(string baseUrlKey, string pathAndQuery, CancellationToken cancellationToken)
    {
        var baseUrl = _configuration[$"ServiceClients:{baseUrlKey}"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return UpstreamResult<JsonElement>.Failure(HttpStatusCode.ServiceUnavailable, $"ServiceClients:{baseUrlKey} is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseUrl), pathAndQuery));
        ForwardBearerToken(request);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return UpstreamResult<JsonElement>.Failure(response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<JsonElement>(stream, JsonOptions, cancellationToken);
        return UpstreamResult<JsonElement>.Success(payload.Clone());
    }

    private void ForwardBearerToken(HttpRequestMessage request)
    {
        var authorization = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
        }
    }
}

public sealed record UpstreamResult<T>(bool IsSuccess, HttpStatusCode StatusCode, T Value, string? Error)
{
    public static UpstreamResult<T> Success(T value) => new(true, HttpStatusCode.OK, value, null);

    public static UpstreamResult<T> Failure(HttpStatusCode statusCode, string? error) => new(false, statusCode, default!, error);
}
