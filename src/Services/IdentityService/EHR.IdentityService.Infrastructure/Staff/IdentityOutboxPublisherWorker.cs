using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class IdentityOutboxPublisherWorker : OutboxPublisherWorkerBase<IdentityDbContext, IdentityOutboxMessageRow>
{
    public IdentityOutboxPublisherWorker(string connectionString, IEventBus eventBus, IOutboxPublisherSignal signal, ILogger<IdentityOutboxPublisherWorker> logger)
        : base(new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options, eventBus, signal, logger)
    {
    }

    protected override DbSet<IdentityOutboxMessageRow> OutboxMessages(IdentityDbContext db) => db.OutboxMessages;
}
