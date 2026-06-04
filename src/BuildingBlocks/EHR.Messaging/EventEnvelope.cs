namespace EHR.Messaging;

public sealed record EventEnvelope(
    Guid EventId,
    string TenantId,
    string Type,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    object Payload);
