using System.Collections.Concurrent;
using EHR.AppointmentService.Application.Appointments;
using EHR.AppointmentService.Domain.Appointments;
using EHR.Messaging;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class InMemoryAppointmentRepository : IAppointmentRepository
{
    private readonly ConcurrentDictionary<Guid, Appointment> _appointments = new();
    private readonly IEventBus _eventBus;

    public InMemoryAppointmentRepository(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddAsync(Appointment appointment, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _appointments[appointment.Id] = appointment;
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    public async Task SaveAsync(Appointment appointment, IntegrationEvent? integrationEvent, CancellationToken cancellationToken)
    {
        _appointments[appointment.Id] = appointment;
        if (integrationEvent is not null)
        {
            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
        }
    }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _appointments.TryGetValue(id, out var appointment);
        return Task.FromResult(appointment);
    }
}
