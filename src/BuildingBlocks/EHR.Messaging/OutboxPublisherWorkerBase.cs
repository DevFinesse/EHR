using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EHR.Messaging;

public abstract class OutboxPublisherWorkerBase<TDbContext, TOutboxMessage> : BackgroundService
    where TDbContext : DbContext
    where TOutboxMessage : OutboxMessageRowBase
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private readonly DbContextOptions<TDbContext> _options;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    protected OutboxPublisherWorkerBase(DbContextOptions<TDbContext> options, IEventBus eventBus, ILogger logger)
    {
        _options = options;
        _eventBus = eventBus;
        _logger = logger;
    }

    protected abstract DbSet<TOutboxMessage> OutboxMessages(TDbContext db);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox publisher failed while processing a batch.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var db = CreateDbContext();
        var messages = await OutboxMessages(db)
            .Where(message => message.ProcessedAt == null && message.Attempts < 10)
            .OrderBy(message => message.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            using var activity = MessagingTelemetry.ActivitySource.StartActivity("outbox publish", ActivityKind.Internal);
            activity?.SetTag("ehr.event_id", message.EventId);
            activity?.SetTag("ehr.tenant_id", message.TenantId);
            activity?.SetTag("ehr.event_type", message.Type);

            var envelope = message.ToEnvelope();
            if (envelope is null)
            {
                message.Attempts++;
                message.LastError = "Outbox payload could not be deserialized.";
                activity?.SetStatus(ActivityStatusCode.Error, message.LastError);
                MessagingTelemetry.OutboxPublishFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", message.Type), MessagingTelemetry.Tag("reason", "deserialize")));
                continue;
            }

            MessagingTelemetry.OutboxPublishAttempts.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            var published = await _eventBus.TryPublishEnvelopeAsync(envelope, cancellationToken);
            message.Attempts++;
            if (published)
            {
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
                MessagingTelemetry.OutboxPublishSuccesses.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type)));
            }
            else
            {
                message.LastError = "Event bus publish failed.";
                activity?.SetStatus(ActivityStatusCode.Error, message.LastError);
                MessagingTelemetry.OutboxPublishFailures.Add(1, MessagingTelemetry.Tags(MessagingTelemetry.Tag("event.type", envelope.Type), MessagingTelemetry.Tag("reason", "publish")));
            }
        }

        if (messages.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private TDbContext CreateDbContext() =>
        (TDbContext)Activator.CreateInstance(typeof(TDbContext), _options)!;
}
