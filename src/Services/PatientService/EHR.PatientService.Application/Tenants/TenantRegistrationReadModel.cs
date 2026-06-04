namespace EHR.PatientService.Application.Tenants;

public sealed record TenantRegistrationReadModel(
    string TenantId,
    Guid HospitalId,
    string Name,
    DateTimeOffset RegisteredAt,
    string CorrelationId);
