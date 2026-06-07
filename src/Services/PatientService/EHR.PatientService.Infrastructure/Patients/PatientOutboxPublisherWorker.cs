using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class PatientOutboxPublisherWorker : OutboxPublisherWorkerBase<PatientDbContext, OutboxMessageRow>
{
    public PatientOutboxPublisherWorker(string connectionString, IEventBus eventBus, IOutboxPublisherSignal signal, ILogger<PatientOutboxPublisherWorker> logger)
        : base(new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(connectionString).Options, eventBus, signal, logger)
    {
    }

    protected override DbSet<OutboxMessageRow> OutboxMessages(PatientDbContext db) => db.OutboxMessages;
}
