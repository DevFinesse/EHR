using EHR.Messaging;
using EHR.SharedKernel;

namespace EHR.Api.Audit;

public sealed class AuditTrail
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;

    public AuditTrail(EhrStore store, IEventBus eventBus)
    {
        _store = store;
        _eventBus = eventBus;
    }

    public async Task RecordAsync(
        TenantContext tenant,
        string action,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken,
        string severity = "Information")
    {
        var record = new AuditRecord(
            Guid.NewGuid(),
            tenant.TenantId,
            action,
            resourceType,
            resourceId,
            severity,
            DateTimeOffset.UtcNow,
            tenant.CorrelationId,
            tenant.UserId);

        _store.AuditRecords.Enqueue(record);

        await _eventBus.PublishAsync(
            new AuditEvent(record.Id, tenant.TenantId, action, resourceType, resourceId, tenant.CorrelationId),
            cancellationToken);
    }
}
