using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EHR.TenantService.Infrastructure.Hospitals;

public sealed class TenantOutboxPublisherWorker : OutboxPublisherWorkerBase<TenantDbContext, TenantOutboxMessageRow>
{
    public TenantOutboxPublisherWorker(string connectionString, IEventBus eventBus, IOutboxPublisherSignal signal, ILogger<TenantOutboxPublisherWorker> logger)
        : base(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options, eventBus, signal, logger)
    {
    }

    protected override DbSet<TenantOutboxMessageRow> OutboxMessages(TenantDbContext db) => db.OutboxMessages;
}
