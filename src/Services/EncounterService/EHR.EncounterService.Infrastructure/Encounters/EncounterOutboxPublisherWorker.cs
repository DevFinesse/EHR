using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class EncounterOutboxPublisherWorker : OutboxPublisherWorkerBase<EncounterDbContext, EncounterOutboxMessageRow>
{
    public EncounterOutboxPublisherWorker(string connectionString, IEventBus eventBus, IOutboxPublisherSignal signal, ILogger<EncounterOutboxPublisherWorker> logger)
        : base(new DbContextOptionsBuilder<EncounterDbContext>().UseNpgsql(connectionString).Options, eventBus, signal, logger)
    {
    }

    protected override DbSet<EncounterOutboxMessageRow> OutboxMessages(EncounterDbContext db) => db.OutboxMessages;
}
