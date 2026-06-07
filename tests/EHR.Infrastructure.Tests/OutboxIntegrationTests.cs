using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using EHR.Messaging;
using EHR.SharedKernel.Authorization;
using EHR.TenantService.Application.Hospitals;
using EHR.TenantService.Domain.Hospitals;
using EHR.TenantService.Infrastructure.Hospitals;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EHR.Infrastructure.Tests;

public sealed class OutboxIntegrationTests
{
    private const string TenantConnectionString = "Host=localhost;Port=5433;Database=ehr_tenant;Username=ehr;Password=ehr_dev_password";
    private const string BootstrapServers = "localhost:9092";
    private const string HospitalRegisteredTopic = "tenant.hospital.registered";

    [Fact]
    public async Task Tenant_outbox_commits_wakes_worker_publishes_to_kafka_and_marks_message_processed()
    {
        if (!Enabled())
        {
            return;
        }

        await TenantDatabaseMigrator.MigrateAsync(TenantConnectionString);
        await EnsureTopicAsync(HospitalRegisteredTopic);

        using var loggerFactory = LoggerFactory.Create(_ => { });
        using var eventBus = new KafkaEventBus(
            new KafkaEventBusOptions(BootstrapServers, PublishTimeoutMilliseconds: 5000, SocketTimeoutMilliseconds: 3000, RequestTimeoutMilliseconds: 3000),
            loggerFactory.CreateLogger<KafkaEventBus>());
        var signal = new OutboxPublisherSignal();
        var repository = new PostgresHospitalRepository(TenantConnectionString, signal);
        var currentUser = new TestCurrentUserContext($"outbox-test-{Guid.NewGuid():N}");
        var handler = new RegisterHospitalHandler(repository, currentUser);
        var worker = new TenantOutboxPublisherWorker(
            TenantConnectionString,
            eventBus,
            signal,
            loggerFactory.CreateLogger<TenantOutboxPublisherWorker>());

        using var workerCancellation = new CancellationTokenSource();
        await worker.StartAsync(workerCancellation.Token);

        try
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var hospital = await handler.HandleAsync(
                new RegisterHospitalCommand($"Outbox Test Hospital {suffix}", "Nigeria", "Lagos", "Enterprise"),
                CancellationToken.None);

            var envelope = await ConsumeEnvelopeAsync(hospital.TenantId, currentUser.CorrelationId);

            Assert.Equal(HospitalRegisteredTopic, envelope.Type);
            Assert.Equal(hospital.TenantId, envelope.TenantId);
            Assert.Equal(currentUser.CorrelationId, envelope.CorrelationId);
            Assert.True(await OutboxMessageWasProcessedAsync(envelope.EventId));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
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

    private static async Task<EventEnvelope> ConsumeEnvelopeAsync(string tenantId, string correlationId)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = $"ehr-outbox-tests-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        consumer.Subscribe(HospitalRegisteredTopic);
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result is null)
            {
                await Task.Yield();
                continue;
            }

            var envelope = JsonSerializer.Deserialize<EventEnvelope>(result.Message.Value);
            if (envelope is not null
                && string.Equals(envelope.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(envelope.CorrelationId, correlationId, StringComparison.Ordinal))
            {
                return envelope;
            }
        }

        throw new TimeoutException($"Kafka event {HospitalRegisteredTopic} was not consumed for tenant {tenantId}.");
    }

    private static async Task<bool> OutboxMessageWasProcessedAsync(Guid eventId)
    {
        await using var connection = new NpgsqlConnection(TenantConnectionString);
        await connection.OpenAsync();

        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var command = new NpgsqlCommand("select processed_at is not null from outbox_messages where event_id = @event_id;", connection);
            command.Parameters.AddWithValue("event_id", eventId);
            var processed = await command.ExecuteScalarAsync();
            if (processed is true)
            {
                return true;
            }

            await Task.Delay(250);
        }

        return false;
    }

    private static bool Enabled() =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_INFRA_TESTS"), "true", StringComparison.OrdinalIgnoreCase);

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(string correlationId)
        {
            CorrelationId = correlationId;
        }

        public string? UserId => "outbox-test-user";

        public string? TenantId => null;

        public string? Role => PlatformRoles.SuperAdmin;

        public IReadOnlyCollection<string> Permissions => [];

        public string CorrelationId { get; }

        public bool IsAuthenticated => true;

        public bool IsSuperAdmin => true;

        public bool HasPermission(string permission) => true;
    }
}
