using Confluent.Kafka;
using Npgsql;

namespace EHR.Infrastructure.Tests;

public sealed class InfrastructureSmokeTests
{
    [Fact]
    public async Task PostgreSql_databases_are_reachable_when_infrastructure_tests_are_enabled()
    {
        if (!Enabled())
        {
            return;
        }

        var connectionStrings = new[]
        {
            "Host=localhost;Port=5433;Database=ehr_tenant;Username=ehr;Password=ehr_dev_password",
            "Host=localhost;Port=5434;Database=ehr_identity;Username=ehr;Password=ehr_dev_password",
            "Host=localhost;Port=5435;Database=ehr_patient;Username=ehr;Password=ehr_dev_password",
            "Host=localhost;Port=5436;Database=ehr_appointment;Username=ehr;Password=ehr_dev_password",
            "Host=localhost;Port=5437;Database=ehr_encounter;Username=ehr;Password=ehr_dev_password",
            "Host=localhost;Port=5438;Database=ehr_audit;Username=ehr;Password=ehr_dev_password"
        };

        foreach (var connectionString in connectionStrings)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("select 1;", connection);
            Assert.Equal(1, await command.ExecuteScalarAsync());
        }
    }

    [Fact]
    public async Task Kafka_can_publish_and_consume_when_infrastructure_tests_are_enabled()
    {
        if (!Enabled())
        {
            return;
        }

        var topic = $"ehr.infrastructure.tests.{Guid.NewGuid():N}";
        var value = Guid.NewGuid().ToString("N");

        using var producer = new ProducerBuilder<string, string>(new ProducerConfig { BootstrapServers = "localhost:9092" }).Build();
        await producer.ProduceAsync(topic, new Message<string, string> { Key = "test", Value = value });

        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = $"ehr-infra-tests-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        }).Build();

        consumer.Subscribe(topic);
        var result = consumer.Consume(TimeSpan.FromSeconds(20));
        Assert.NotNull(result);
        Assert.Equal(value, result.Message.Value);
    }

    private static bool Enabled() =>
        string.Equals(Environment.GetEnvironmentVariable("RUN_INFRA_TESTS"), "true", StringComparison.OrdinalIgnoreCase);
}
