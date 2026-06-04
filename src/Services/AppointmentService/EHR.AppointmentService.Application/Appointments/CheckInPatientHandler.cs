using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.Messaging;
using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;

namespace EHR.AppointmentService.Application.Appointments;

public sealed class CheckInPatientHandler : ICommandHandler<CheckInPatientCommand, Result<Appointment>>
{
    private readonly IAppointmentRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public CheckInPatientHandler(IAppointmentRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Result<Appointment>> HandleAsync(CheckInPatientCommand command, CancellationToken cancellationToken)
    {
        var appointment = await _repository.GetByIdAsync(command.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result<Appointment>.Failure("Appointment was not found.");
        }

        _tenantAuthorization.EnsureCanAccessTenant(appointment.TenantId);

        appointment.CheckIn();
        var integrationEvent = new PatientCheckedInEvent(Guid.NewGuid(), appointment.TenantId, appointment.Id, appointment.PatientId, Guid.NewGuid().ToString("N"));
        await _repository.SaveAsync(appointment, integrationEvent, cancellationToken);
        return Result<Appointment>.Success(appointment);
    }
}
