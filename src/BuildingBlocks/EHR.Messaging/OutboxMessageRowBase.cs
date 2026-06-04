using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EHR.Messaging;

public abstract class OutboxMessageRowBase
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    protected void Apply(IntegrationEvent integrationEvent)
    {
        var envelope = new EventEnvelope(
            integrationEvent.EventId,
            integrationEvent.TenantId,
            integrationEvent.Type,
            integrationEvent.OccurredAt,
            integrationEvent.CorrelationId,
            integrationEvent);

        Id = Guid.NewGuid();
        EventId = envelope.EventId;
        TenantId = envelope.TenantId;
        Type = envelope.Type;
        OccurredAt = envelope.OccurredAt;
        CorrelationId = envelope.CorrelationId;
        Payload = JsonSerializer.Serialize(envelope);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public EventEnvelope? ToEnvelope() => JsonSerializer.Deserialize<EventEnvelope>(Payload);
}

public static class OutboxMessageMapping
{
    public static void MapOutboxMessage<TEntity>(this EntityTypeBuilder<TEntity> entity)
        where TEntity : OutboxMessageRowBase
    {
        entity.ToTable("outbox_messages");
        entity.HasKey(row => row.Id);
        entity.HasIndex(row => new { row.ProcessedAt, row.OccurredAt });
        entity.Property(row => row.Id).HasColumnName("id");
        entity.Property(row => row.EventId).HasColumnName("event_id");
        entity.Property(row => row.TenantId).HasColumnName("tenant_id");
        entity.Property(row => row.Type).HasColumnName("type");
        entity.Property(row => row.OccurredAt).HasColumnName("occurred_at");
        entity.Property(row => row.CorrelationId).HasColumnName("correlation_id");
        entity.Property(row => row.Payload).HasColumnName("payload").HasColumnType("jsonb");
        entity.Property(row => row.Attempts).HasColumnName("attempts");
        entity.Property(row => row.LastError).HasColumnName("last_error");
        entity.Property(row => row.ProcessedAt).HasColumnName("processed_at");
        entity.Property(row => row.CreatedAt).HasColumnName("created_at");
    }
}
