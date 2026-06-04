using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

                var handlers = _handlers.Where(handler => handler.EventType == envelope.Type).ToArray();
                foreach (var handler in handlers)
                {
                    await handler.HandleAsync(envelope, stoppingToken);
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Kafka consumer failed while processing a message.");
            }
        }
    }
}
