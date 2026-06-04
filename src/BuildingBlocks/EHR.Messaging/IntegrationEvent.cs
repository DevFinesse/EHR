namespace EHR.Messaging;

public abstract record IntegrationEvent(
    Guid EventId,
    string TenantId,
    string Type,
    DateTimeOffset OccurredAt,
    string CorrelationId);
