using System.Collections.Concurrent;

namespace EHR.Messaging;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentQueue<EventEnvelope> _events = new();

    public IReadOnlyCollection<EventEnvelope> PublishedEvents => _events.ToArray();

    public Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        return TryPublishEnvelopeAsync(new EventEnvelope(
            integrationEvent.EventId,
            integrationEvent.TenantId,
            integrationEvent.Type,
            integrationEvent.OccurredAt,
            integrationEvent.CorrelationId,
            integrationEvent), cancellationToken);
    }

    public Task<bool> TryPublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        _events.Enqueue(envelope);

        return Task.FromResult(true);
    }
}
