using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class AppointmentOutboxPublisherWorker : OutboxPublisherWorkerBase<AppointmentDbContext, AppointmentOutboxMessageRow>
{
    public AppointmentOutboxPublisherWorker(string connectionString, IEventBus eventBus, IOutboxPublisherSignal signal, ILogger<AppointmentOutboxPublisherWorker> logger)
        : base(new DbContextOptionsBuilder<AppointmentDbContext>().UseNpgsql(connectionString).Options, eventBus, signal, logger)
    {
    }

    protected override DbSet<AppointmentOutboxMessageRow> OutboxMessages(AppointmentDbContext db) => db.OutboxMessages;
}
