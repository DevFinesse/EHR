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
    private readonly ILogger<KafkaConsumerWorker> _logger;

    public KafkaConsumerWorker(IConfiguration configuration, IEnumerable<IIntegrationEventHandler> handlers, ILogger<KafkaConsumerWorker> logger)
    {
        _configuration = configuration;
        _handlers = handlers;
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

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
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
                    var handlers = _handlers.Where(handler => handler.EventType == envelope.Type).ToArray();
                    foreach (var handler in handlers)
                    {
                        await handler.HandleAsync(envelope, stoppingToken);
                    }

                    MessagingTelemetry.KafkaConsumedMessages.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    MessagingTelemetry.KafkaConsumeDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    consumer.Commit(result);
                }
                catch (Exception exception)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    MessagingTelemetry.KafkaConsumeFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    MessagingTelemetry.KafkaConsumeDuration.Record(ElapsedMilliseconds(started), MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("consumer.group", groupId)));
                    throw;
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
}
