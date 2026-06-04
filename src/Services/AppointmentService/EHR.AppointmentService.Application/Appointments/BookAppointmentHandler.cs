using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.Messaging;
using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;

namespace EHR.AppointmentService.Application.Appointments;

public sealed class BookAppointmentHandler : ICommandHandler<BookAppointmentCommand, Result<Appointment>>
{
    private readonly IAppointmentRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public BookAppointmentHandler(IAppointmentRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Result<Appointment>> HandleAsync(BookAppointmentCommand command, CancellationToken cancellationToken)
    {
        if (command.ScheduledFor < DateTimeOffset.UtcNow.AddMinutes(-5))
        {
            return Result<Appointment>.Failure("Appointment time cannot be in the past.");
        }

        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var appointment = Appointment.Book(tenantId, command.PatientId, command.PractitionerId, command.ScheduledFor, command.Reason);
        var integrationEvent = new AppointmentBookedEvent(Guid.NewGuid(), appointment.TenantId, appointment.Id, appointment.PatientId, appointment.PractitionerId, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(appointment, integrationEvent, cancellationToken);
        return Result<Appointment>.Success(appointment);
    }
}
