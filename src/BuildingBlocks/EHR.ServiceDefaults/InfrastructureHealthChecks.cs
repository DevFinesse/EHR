using System.Net.Sockets;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace EHR.ServiceDefaults;

internal sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public PostgresHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("select 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is not reachable.", exception);
        }
    }
}

internal sealed class KafkaHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;

    public KafkaHealthCheck(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers,
                SocketTimeoutMs = 2000
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(2));
            return Task.FromResult(metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy("Kafka broker metadata is reachable.")
                : HealthCheckResult.Unhealthy("Kafka returned no brokers."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka is not reachable.", exception));
        }
    }
}

internal sealed class SmtpHealthCheck : IHealthCheck
{
    private readonly string _host;
    private readonly int _port;

    public SmtpHealthCheck(string host, int port)
    {
        _host = host;
        _port = port;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(_host, _port, cancellationToken);
            return HealthCheckResult.Healthy("SMTP endpoint is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("SMTP endpoint is not reachable.", exception);
        }
    }
}
