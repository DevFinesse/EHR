using EHR.Messaging;
using EHR.AppointmentService.Domain.Appointments;

namespace EHR.AppointmentService.Application.Appointments;

public interface IAppointmentRepository
{
    Task AddAsync(Appointment appointment, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task SaveAsync(Appointment appointment, IntegrationEvent? integrationEvent, CancellationToken cancellationToken);

    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
