using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EHR.Messaging;
using EHR.PatientService.Infrastructure.Patients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Npgsql;

namespace EHR.Infrastructure.Tests;

public sealed class ConsumerReliabilityIntegrationTests
{
    private const string PatientConnectionString = "Host=localhost;Port=5435;Database=ehr_patient;Username=ehr;Password=ehr_dev_password";
    private const string BootstrapServers = "localhost:9092";

    [Fact]
    public async Task Kafka_consumer_retries_records_inbox_and_dead_letters_poison_messages()
    {
        if (!Enabled())
        {
            return;
        }

        await PatientDatabaseMigrator.MigrateAsync(PatientConnectionString);

        var topic = $"ehr.consumer.tests.{Guid.NewGuid():N}";
        var deadLetterTopic = topic + ".dlq";
        await EnsureTopicAsync(topic);
        await EnsureTopicAsync(deadLetterTopic);

        var consumerGroup = $"ehr-consumer-tests-{Guid.NewGuid():N}";
        using var loggerFactory = LoggerFactory.Create(_ => { });
        var configuration = new DictionaryConfiguration(new Dictionary<string, string?>
        {
            ["Kafka:BootstrapServers"] = BootstrapServers,
            ["Kafka:ConsumerGroupId"] = consumerGroup,
            ["Kafka:ConsumerTopics:0"] = topic,
            ["Kafka:ConsumerMaxAttempts"] = "2",
            ["Kafka:ConsumerRetryDelayMilliseconds"] = "25",
            ["Kafka:DeadLetterPublishTimeoutMilliseconds"] = "5000"
        });

        var handler = new AlwaysFailingIntegrationEventHandler(topic);
        var inbox = new PostgresInboxStore(PatientConnectionString);
        var worker = new KafkaConsumerWorker(
            configuration,
            [handler],
            inbox,
            loggerFactory.CreateLogger<KafkaConsumerWorker>());

        using var workerCancellation = new CancellationTokenSource();
        await worker.StartAsync(workerCancellation.Token);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "tenant-consumer-test",
            topic,
            DateTimeOffset.UtcNow,
            $"consumer-test-{Guid.NewGuid():N}",
            new { reason = "poison" });

        try
        {
            await ProduceAsync(topic, envelope);
            var deadLetter = await ConsumeDeadLetterAsync(deadLetterTopic, envelope.EventId);

            Assert.Equal(envelope.EventId, deadLetter.OriginalEnvelope.EventId);
            Assert.Equal(2, deadLetter.Attempts);
            Assert.Equal(nameof(InvalidOperationException), deadLetter.ExceptionType);
            Assert.True(await InboxMessageHasStatusAsync(envelope.EventId, consumerGroup, "dead-lettered", 2));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    private static async Task ProduceAsync(string topic, EventEnvelope envelope)
    {
        using var producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        await producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = envelope.TenantId,
            Value = JsonSerializer.Serialize(envelope)
        });
    }

    private static async Task<DeadLetterEnvelope> ConsumeDeadLetterAsync(string topic, Guid eventId)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = $"ehr-dlq-tests-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        consumer.Subscribe(topic);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result is null)
            {
                await Task.Yield();
                continue;
            }

            var deadLetter = JsonSerializer.Deserialize<DeadLetterEnvelope>(result.Message.Value);
            if (deadLetter?.OriginalEnvelope.EventId == eventId)
            {
                return deadLetter;
            }
        }

        throw new TimeoutException($"Dead-letter message was not consumed from {topic} for event {eventId}.");
    }

    private static async Task<bool> InboxMessageHasStatusAsync(Guid eventId, string consumerGroup, string status, int attempts)
    {
        await using var connection = new NpgsqlConnection(PatientConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            select status, attempts
            from inbox_messages
            where event_id = @event_id and consumer_group = @consumer_group;
            """, connection);
        command.Parameters.AddWithValue("event_id", eventId);
        command.Parameters.AddWithValue("consumer_group", consumerGroup);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return false;
        }

        return string.Equals(reader.GetString(0), status, StringComparison.OrdinalIgnoreCase)
            && reader.GetInt32(1) == attempts;
    }

    private static async Task EnsureTopicAsync(string topic)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = BootstrapServers }).Build();

        try
        {
            await admin.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            ]);
        }
        catch (CreateTopicsException exception) when (exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
        }
    }

    private static bool Enabled() =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_INFRA_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    private sealed class AlwaysFailingIntegrationEventHandler : IIntegrationEventHandler
    {
        public AlwaysFailingIntegrationEventHandler(string eventType)
        {
            EventType = eventType;
        }

        public string EventType { get; }

        public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Intentional poison message failure.");
    }

    private sealed class DictionaryConfiguration : IConfiguration
    {
        private readonly IReadOnlyDictionary<string, string?> _values;
        private readonly string _path;

        public DictionaryConfiguration(IReadOnlyDictionary<string, string?> values, string path = "")
        {
            _values = values;
            _path = path;
        }

        public string? this[string key]
        {
            get => _values.TryGetValue(Qualify(key), out var value) ? value : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            var prefix = string.IsNullOrWhiteSpace(_path) ? string.Empty : _path + ":";
            return _values.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(key => key[prefix.Length..].Split(':')[0])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(key => new DictionaryConfigurationSection(_values, string.IsNullOrWhiteSpace(_path) ? key : _path + ":" + key));
        }

        public IChangeToken GetReloadToken() => NoopChangeToken.Instance;

        public IConfigurationSection GetSection(string key) =>
            new DictionaryConfigurationSection(_values, Qualify(key));

        private string Qualify(string key) =>
            string.IsNullOrWhiteSpace(_path) ? key : _path + ":" + key;
    }

    private sealed class DictionaryConfigurationSection : IConfigurationSection
    {
        private readonly IReadOnlyDictionary<string, string?> _values;
        private readonly DictionaryConfiguration _configuration;

        public DictionaryConfigurationSection(IReadOnlyDictionary<string, string?> values, string path)
        {
            _values = values;
            Path = path;
            Key = path.Split(':').Last();
            _configuration = new DictionaryConfiguration(values, path);
        }

        public string Key { get; }

        public string Path { get; }

        public string? Value { get => _values.TryGetValue(Path, out var value) ? value : null; set => throw new NotSupportedException(); }

        public string? this[string key]
        {
            get => _configuration[key];
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => _configuration.GetChildren();

        public IChangeToken GetReloadToken() => NoopChangeToken.Instance;

        public IConfigurationSection GetSection(string key) => _configuration.GetSection(key);
    }

    private sealed class NoopChangeToken : IChangeToken
    {
        public static readonly NoopChangeToken Instance = new();

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
