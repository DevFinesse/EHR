namespace EHR.Messaging;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task<bool> TryPublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken);

    IReadOnlyCollection<EventEnvelope> PublishedEvents { get; }
}
