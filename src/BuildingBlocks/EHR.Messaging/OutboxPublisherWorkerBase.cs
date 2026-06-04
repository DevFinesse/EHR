using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
            var envelope = message.ToEnvelope();
            if (envelope is null)
            {
                message.Attempts++;
                message.LastError = "Outbox payload could not be deserialized.";
                continue;
            }

            var published = await _eventBus.TryPublishEnvelopeAsync(envelope, cancellationToken);
            message.Attempts++;
            if (published)
            {
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            else
            {
                message.LastError = "Event bus publish failed.";
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
