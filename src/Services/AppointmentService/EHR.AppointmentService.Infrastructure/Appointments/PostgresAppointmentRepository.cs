using EHR.AppointmentService.Application.Appointments;
using EHR.AppointmentService.Domain.Appointments;
using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class PostgresAppointmentRepository : IAppointmentRepository
{
    private readonly DbContextOptions<AppointmentDbContext> _options;
    private readonly IOutboxPublisherSignal _outboxSignal;

    public PostgresAppointmentRepository(string connectionString, IOutboxPublisherSignal outboxSignal)
    {
        _options = new DbContextOptionsBuilder<AppointmentDbContext>().UseNpgsql(connectionString).Options;
        _outboxSignal = outboxSignal;
    }

    public async Task AddAsync(Appointment appointment, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await SaveAsync(appointment, integrationEvent, cancellationToken);
    }

    public async Task SaveAsync(Appointment appointment, IntegrationEvent? integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = new AppointmentDbContext(_options);
        var row = await db.Appointments.SingleOrDefaultAsync(existing => existing.Id == appointment.Id, cancellationToken);
        if (row is null)
        {
            db.Appointments.Add(new AppointmentRow
            {
                Id = appointment.Id,
                TenantId = appointment.TenantId,
                PatientId = appointment.PatientId,
                PractitionerId = appointment.PractitionerId,
                ScheduledFor = appointment.ScheduledFor,
                Reason = appointment.Reason,
                Status = appointment.Status
            });
        }
        else
        {
            row.Status = appointment.Status;
        }

        if (integrationEvent is not null)
        {
            db.OutboxMessages.Add(AppointmentOutboxMessageRow.FromIntegrationEvent(integrationEvent));
        }

        await db.SaveChangesAsync(cancellationToken);
        if (integrationEvent is not null)
        {
            _outboxSignal.Signal();
        }
    }

    public async Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = new AppointmentDbContext(_options);
        var row = await db.Appointments.AsNoTracking().SingleOrDefaultAsync(appointment => appointment.Id == id, cancellationToken);

        return row is null
            ? null
            : Appointment.Restore(row.Id, row.TenantId, row.PatientId, row.PractitionerId, row.ScheduledFor, row.Reason, row.Status);
    }
}
