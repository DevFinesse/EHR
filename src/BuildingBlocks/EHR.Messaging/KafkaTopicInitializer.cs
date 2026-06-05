using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EHR.Messaging;

public sealed class KafkaTopicInitializer : IHostedService
{
    private static readonly string[] DefaultTopics =
    [
        "tenant.hospital.registered",
        "identity.staff.created",
        "patient.created",
        "appointment.booked",
        "patient.checked_in",
        "encounter.started",
        "vitals.recorded",
        "diagnosis.added",
        "encounter.completed",
        "audit.event"
    ];

    private readonly string _bootstrapServers;
    private readonly string[] _configuredTopics;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(string bootstrapServers, IEnumerable<string> configuredTopics, ILogger<KafkaTopicInitializer> logger)
    {
        _bootstrapServers = bootstrapServers;
        _configuredTopics = configuredTopics
            .Concat(DefaultTopics)
            .Where(topic => !string.IsNullOrWhiteSpace(topic))
            .Select(topic => topic.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_configuredTopics.Length == 0)
        {
            return;
        }

        using var activity = MessagingTelemetry.ActivitySource.StartActivity("kafka topic initialize", ActivityKind.Internal);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.kafka.topic_count", _configuredTopics.Length);

        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _bootstrapServers,
            SocketTimeoutMs = 3000
        }).Build();

        try
        {
            await adminClient.CreateTopicsAsync(_configuredTopics.Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            }));

            MessagingTelemetry.KafkaTopicsCreated.Add(_configuredTopics.Length);
            _logger.LogInformation("Kafka topics ensured: {Topics}", string.Join(", ", _configuredTopics));
        }
        catch (CreateTopicsException exception)
        {
            var unexpected = exception.Results
                .Where(result => result.Error.Code != ErrorCode.TopicAlreadyExists)
                .ToArray();

            if (unexpected.Length == 0)
            {
                _logger.LogInformation("Kafka topics already exist: {Topics}", string.Join(", ", _configuredTopics));
                return;
            }

            activity?.SetStatus(ActivityStatusCode.Error, "Kafka topic initialization partially failed.");
            MessagingTelemetry.KafkaTopicInitializationFailures.Add(unexpected.Length);
            foreach (var result in unexpected)
            {
                _logger.LogWarning("Kafka topic {Topic} could not be created: {Reason}", result.Topic, result.Error.Reason);
            }
        }
        catch (KafkaException exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Error.Reason);
            MessagingTelemetry.KafkaTopicInitializationFailures.Add(1);
            _logger.LogWarning(exception, "Kafka topic initialization failed: {Reason}", exception.Error.Reason);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
