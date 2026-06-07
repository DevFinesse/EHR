using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EHR.Messaging;

public interface IIntegrationEventHandler
{
    string EventType { get; }

    Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken);
}

public sealed class KafkaConsumerWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IEnumerable<IIntegrationEventHandler> _handlers;
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<KafkaConsumerWorker> _logger;

    public KafkaConsumerWorker(IConfiguration configuration, IEnumerable<IIntegrationEventHandler> handlers, IInboxStore inboxStore, ILogger<KafkaConsumerWorker> logger)
    {
        _configuration = configuration;
        _handlers = handlers;
        _inboxStore = inboxStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        var groupId = _configuration["Kafka:ConsumerGroupId"];
        var topics = _configuration
            .GetSection("Kafka:ConsumerTopics")
            .GetChildren()
            .Select(topic => topic.Value)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Cast<string>()
            .ToArray();

        if (string.IsNullOrWhiteSpace(bootstrapServers) || string.IsNullOrWhiteSpace(groupId) || topics.Length == 0)
        {
            return;
        }

        var maxAttempts = ReadInt("Kafka:ConsumerMaxAttempts", 5);
        var retryDelay = TimeSpan.FromMilliseconds(ReadInt("Kafka:ConsumerRetryDelayMilliseconds", 1000));
        var deadLetterSuffix = _configuration["Kafka:DeadLetterTopicSuffix"] ?? ".dlq";

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();
        using var deadLetterProducer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = ReadInt("Kafka:DeadLetterPublishTimeoutMilliseconds", 5000)
        }).Build();

        consumer.Subscribe(topics);
        _logger.LogInformation("Kafka consumer subscribed to {Topics}", string.Join(", ", topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Message.Value);
                if (envelope is null)
                {
                    consumer.Commit(result);
                    continue;
                }

                using var activity = MessagingTelemetry.ActivitySource.StartActivity("kafka consume", ActivityKind.Consumer);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", result.Topic);
                activity?.SetTag("messaging.kafka.consumer.group", groupId);
                activity?.SetTag("messaging.kafka.partition", result.Partition.Value);
                activity?.SetTag("messaging.kafka.offset", result.Offset.Value);
                activity?.SetTag("ehr.event_id", envelope.EventId);
                activity?.SetTag("ehr.tenant_id", envelope.TenantId);
                activity?.SetTag("ehr.correlation_id", envelope.CorrelationId);

                var started = Stopwatch.GetTimestamp();
                try
                {
                    var inboxStatus = await _inboxStore.TryStartProcessingAsync(
                        envelope,
                        groupId,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        stoppingToken);
                    if (inboxStatus == InboxProcessingStatus.AlreadyProcessed)
                    {
                        consumer.Commit(result);
                        continue;
                    }

                    var handlers = _handlers.Where(handler => handler.EventType == envelope.Type).ToArray();
                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(envelope, stoppingToken);
                    }

                    await _inboxStore.MarkProcessedAsync(envelope.EventId, groupId, stoppingToken);
                    MessagingTelemetry.KafkaConsumedMessages.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    MessagingTelemetry.KafkaConsumeDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    consumer.Commit(result);
                }
                catch (Exception exception)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    MessagingTelemetry.KafkaConsumeFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    MessagingTelemetry.KafkaConsumeDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    var failure = await _inboxStore.RecordFailureAsync(
                        envelope,
                        groupId,
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        exception,
                        maxAttempts,
                        stoppingToken);

                    if (failure.ShouldDeadLetter)
                    {
                        var published = await TryPublishDeadLetterAsync(
                            deadLetterProducer,
                            envelope,
                            result,
                            groupId,
                            failure.Attempts,
                            exception,
                            deadLetterSuffix,
                            stoppingToken);
                        if (published)
                        {
                            consumer.Commit(result);
                        }
                    }
                    else
                    {
                        consumer.Seek(result.TopicPartitionOffset);
                        await Task.Delay(retryDelay, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException exception) when (exception.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogWarning(
                    "Kafka topic is not available yet for consumer group {GroupId}: {Reason}",
                    groupId,
                    exception.Error.Reason);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kafka consumer failed while processing a message.");
            }
        }
    }

    private static double ElapsedMilliseconds(long started) =>
        (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;

    private int ReadInt(string key, int fallback) =>
        int.TryParse(_configuration[key], out var value) ? value : fallback;

    private async Task<bool> TryPublishDeadLetterAsync(
        IProducer<string, string> producer,
        EventEnvelope envelope,
        ConsumeResult<string, string> result,
        string consumerGroup,
        int attempts,
        Exception exception,
        string deadLetterSuffix,
        CancellationToken cancellationToken)
    {
        var deadLetter = new DeadLetterEnvelope(
            Guid.NewGuid(),
            result.Topic,
            result.Partition.Value,
            result.Offset.Value,
            consumerGroup,
            attempts,
            DateTimeOffset.UtcNow,
            exception.GetType().Name,
            exception.Message,
            envelope);

        var topic = envelope.Type + deadLetterSuffix;
        try
        {
            await producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = envelope.TenantId,
                    Value = JsonSerializer.Serialize(deadLetter)
                },
                cancellationToken);

            _logger.LogWarning(
                "Kafka message {EventType} {EventId} was dead-lettered to {DeadLetterTopic} after {Attempts} attempts.",
                envelope.Type,
                envelope.EventId,
                topic,
                attempts);
            return true;
        }
        catch (Exception publishException) when (publishException is ProduceException<string, string> or KafkaException or OperationCanceledException)
        {
            _logger.LogError(
                publishException,
                "Failed to publish dead-letter message for event {EventType} {EventId}. The original Kafka offset will not be committed.",
                envelope.Type,
                envelope.EventId);
            return false;
        }
    }
}
