using EHR.AuditService.Application.AuditRecords;
using EHR.AuditService.Domain.AuditRecords;
using EHR.Messaging;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public sealed class AuditIntegrationEventHandler : IIntegrationEventHandler
{
    private readonly IAuditRecordRepository _repository;

    public AuditIntegrationEventHandler(IAuditRecordRepository repository, string eventType)
    {
        _repository = repository;
        EventType = eventType;
    }

    public string EventType { get; }

    public async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var record = AuditRecord.Create(
            envelope.TenantId,
            envelope.Type,
            "IntegrationEvent",
            envelope.EventId.ToString(),
            "Information",
            envelope.CorrelationId,
            null);

        await _repository.AddAsync(record, cancellationToken);
    }
}
