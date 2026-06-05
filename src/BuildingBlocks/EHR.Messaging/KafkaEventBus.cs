using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EHR.Messaging;

public sealed class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaEventBusOptions _options;
    private readonly ILogger<KafkaEventBus> _logger;

    public KafkaEventBus(KafkaEventBusOptions options, ILogger<KafkaEventBus> logger)
    {
        _options = options;
        _logger = logger;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = options.PublishTimeoutMilliseconds,
            SocketTimeoutMs = options.SocketTimeoutMilliseconds,
            RequestTimeoutMs = options.RequestTimeoutMilliseconds,
            MessageSendMaxRetries = Math.Max(options.MessageSendMaxRetries, 1),
            RetryBackoffMs = options.RetryBackoffMilliseconds
        }).Build();
    }

    public IReadOnlyCollection<EventEnvelope> PublishedEvents => [];

    public async Task PublishAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var envelope = new EventEnvelope(
            integrationEvent.EventId,
            integrationEvent.TenantId,
            integrationEvent.Type,
            integrationEvent.OccurredAt,
            integrationEvent.CorrelationId,
            integrationEvent);

        await TryPublishEnvelopeAsync(envelope, cancellationToken);
    }

    public async Task<bool> TryPublishEnvelopeAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        using var activity = MessagingTelemetry.ActivitySource.StartActivity("kafka publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", envelope.Type);
        activity?.SetTag("messaging.kafka.message.key", envelope.TenantId);
        activity?.SetTag("ehr.event_id", envelope.EventId);
        activity?.SetTag("ehr.tenant_id", envelope.TenantId);
        activity?.SetTag("ehr.correlation_id", envelope.CorrelationId);

        var started = Stopwatch.GetTimestamp();
        MessagingTelemetry.KafkaPublishAttempts.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(_options.PublishTimeoutMilliseconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            await _producer.ProduceAsync(
                envelope.Type,
                new Message<string, string>
                {
                    Key = envelope.TenantId,
                    Value = JsonSerializer.Serialize(envelope)
                },
                linked.Token);

            MessagingTelemetry.KafkaPublishDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Kafka publish timed out.");
            MessagingTelemetry.KafkaPublishFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("reason", "timeout")));
            MessagingTelemetry.KafkaPublishDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            _logger.LogWarning(
                "Kafka publish timed out after {TimeoutMilliseconds} ms for event {EventType} {EventId}.",
                _options.PublishTimeoutMilliseconds,
                envelope.Type,
                envelope.EventId);

            return false;
        }
        catch (ProduceException<string, string> exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Error.Reason);
            MessagingTelemetry.KafkaPublishFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("reason", exception.Error.Code.ToString())));
            MessagingTelemetry.KafkaPublishDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            _logger.LogWarning(
                exception,
                "Kafka publish failed for event {EventType} {EventId}: {Reason}.",
                envelope.Type,
                envelope.EventId,
                exception.Error.Reason);

            return false;
        }
        catch (KafkaException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Error.Reason);
            MessagingTelemetry.KafkaPublishFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("reason", exception.Error.Code.ToString())));
            MessagingTelemetry.KafkaPublishDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            _logger.LogWarning(
                exception,
                "Kafka publish failed for event {EventType} {EventId}: {Reason}.",
                envelope.Type,
                envelope.EventId,
                exception.Error.Reason);

            return false;
        }
    }

    private static double ElapsedMilliseconds(long started) =>
        (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}

public sealed record KafkaEventBusOptions(
    string BootstrapServers,
    int PublishTimeoutMilliseconds = 1500,
    int SocketTimeoutMilliseconds = 1000,
    int RequestTimeoutMilliseconds = 1000,
    int MessageSendMaxRetries = 1,
    int RetryBackoffMilliseconds = 100);
