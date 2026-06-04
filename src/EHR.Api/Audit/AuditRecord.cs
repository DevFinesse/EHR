namespace EHR.Api.Audit;

public sealed record AuditRecord(
    Guid Id,
    string TenantId,
    string Action,
    string ResourceType,
    string ResourceId,
    string Severity,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? UserId);
