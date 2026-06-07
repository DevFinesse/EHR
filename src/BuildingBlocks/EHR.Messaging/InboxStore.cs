using Npgsql;

namespace EHR.Messaging;

public enum InboxProcessingStatus
{
    ShouldProcess,
    AlreadyProcessed
}

public interface IInboxStore
{
    Task<InboxProcessingStatus> TryStartProcessingAsync(
        EventEnvelope envelope,
        string consumerGroup,
        string topic,
        int partition,
        long offset,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(Guid eventId, string consumerGroup, CancellationToken cancellationToken);

    Task<InboxFailureResult> RecordFailureAsync(
        EventEnvelope envelope,
        string consumerGroup,
        string topic,
        int partition,
        long offset,
        Exception exception,
        int maxAttempts,
        CancellationToken cancellationToken);
}

public sealed record InboxFailureResult(int Attempts, bool ShouldDeadLetter);

public sealed class NoopInboxStore : IInboxStore
{
    public Task<InboxProcessingStatus> TryStartProcessingAsync(EventEnvelope envelope, string consumerGroup, string topic, int partition, long offset, CancellationToken cancellationToken) =>
        Task.FromResult(InboxProcessingStatus.ShouldProcess);

    public Task MarkProcessedAsync(Guid eventId, string consumerGroup, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<InboxFailureResult> RecordFailureAsync(EventEnvelope envelope, string consumerGroup, string topic, int partition, long offset, Exception exception, int maxAttempts, CancellationToken cancellationToken) =>
        Task.FromResult(new InboxFailureResult(1, false));
}

public sealed class PostgresInboxStore : IInboxStore
{
    private readonly string _connectionString;

    public PostgresInboxStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<InboxProcessingStatus> TryStartProcessingAsync(
        EventEnvelope envelope,
        string consumerGroup,
        string topic,
        int partition,
        long offset,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var insert = new NpgsqlCommand("""
            insert into inbox_messages (
                event_id,
                consumer_group,
                event_type,
                tenant_id,
                correlation_id,
                topic,
                partition,
                offset_value,
                attempts,
                status,
                received_at)
            values (
                @event_id,
                @consumer_group,
                @event_type,
                @tenant_id,
                @correlation_id,
                @topic,
                @partition,
                @offset_value,
                0,
                'processing',
                now())
            on conflict (event_id, consumer_group) do nothing;
            """, connection))
        {
            AddCommonParameters(insert, envelope, consumerGroup, topic, partition, offset);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var select = new NpgsqlCommand("""
            select status
            from inbox_messages
            where event_id = @event_id and consumer_group = @consumer_group;
            """, connection);
        select.Parameters.AddWithValue("event_id", envelope.EventId);
        select.Parameters.AddWithValue("consumer_group", consumerGroup);
        var status = (string?)await select.ExecuteScalarAsync(cancellationToken);

        return string.Equals(status, "processed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "dead-lettered", StringComparison.OrdinalIgnoreCase)
                ? InboxProcessingStatus.AlreadyProcessed
                : InboxProcessingStatus.ShouldProcess;
    }

    public async Task MarkProcessedAsync(Guid eventId, string consumerGroup, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            update inbox_messages
            set status = 'processed',
                processed_at = now(),
                last_error = null
            where event_id = @event_id and consumer_group = @consumer_group;
            """, connection);
        command.Parameters.AddWithValue("event_id", eventId);
        command.Parameters.AddWithValue("consumer_group", consumerGroup);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<InboxFailureResult> RecordFailureAsync(
        EventEnvelope envelope,
        string consumerGroup,
        string topic,
        int partition,
        long offset,
        Exception exception,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            update inbox_messages
            set attempts = attempts + 1,
                status = case when attempts + 1 >= @max_attempts then 'dead-lettered' else 'failed' end,
                last_error = @last_error,
                topic = @topic,
                partition = @partition,
                offset_value = @offset_value,
                dead_lettered_at = case when attempts + 1 >= @max_attempts then now() else dead_lettered_at end
            where event_id = @event_id and consumer_group = @consumer_group
            returning attempts, status;
            """, connection);
        command.Parameters.AddWithValue("max_attempts", Math.Max(maxAttempts, 1));
        command.Parameters.AddWithValue("last_error", $"{exception.GetType().Name}: {exception.Message}");
        AddCommonParameters(command, envelope, consumerGroup, topic, partition, offset);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new InboxFailureResult(0, false);
        }

        var attempts = reader.GetInt32(0);
        var status = reader.GetString(1);
        return new InboxFailureResult(attempts, string.Equals(status, "dead-lettered", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddCommonParameters(NpgsqlCommand command, EventEnvelope envelope, string consumerGroup, string topic, int partition, long offset)
    {
        command.Parameters.AddWithValue("event_id", envelope.EventId);
        command.Parameters.AddWithValue("consumer_group", consumerGroup);
        command.Parameters.AddWithValue("event_type", envelope.Type);
        command.Parameters.AddWithValue("tenant_id", envelope.TenantId);
        command.Parameters.AddWithValue("correlation_id", envelope.CorrelationId);
        command.Parameters.AddWithValue("topic", topic);
        command.Parameters.AddWithValue("partition", partition);
        command.Parameters.AddWithValue("offset_value", offset);
    }
}
