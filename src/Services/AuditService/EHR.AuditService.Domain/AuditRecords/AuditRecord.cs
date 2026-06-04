namespace EHR.AuditService.Domain.AuditRecords;

public sealed class AuditRecord
{
    private AuditRecord(Guid id, string tenantId, string action, string resourceType, string resourceId, string severity, string correlationId, string? userId)
    {
        Id = id;
        TenantId = tenantId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        Severity = severity;
        CorrelationId = correlationId;
        UserId = userId;
        Timestamp = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string Action { get; }
    public string ResourceType { get; }
    public string ResourceId { get; }
    public string Severity { get; }
    public DateTimeOffset Timestamp { get; }
    public string CorrelationId { get; }
    public string? UserId { get; }

    public static AuditRecord Create(string tenantId, string action, string resourceType, string resourceId, string severity, string correlationId, string? userId) =>
        new(Guid.NewGuid(), tenantId.Trim(), action.Trim(), resourceType.Trim(), resourceId.Trim(), severity.Trim(), correlationId.Trim(), userId);

    public static AuditRecord Restore(Guid id, string tenantId, string action, string resourceType, string resourceId, string severity, string correlationId, string? userId) =>
        new(id, tenantId, action, resourceType, resourceId, severity, correlationId, userId);
}
