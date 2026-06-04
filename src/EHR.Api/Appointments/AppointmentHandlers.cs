using EHR.Api.Audit;
using EHR.Messaging;
using EHR.SharedKernel;

namespace EHR.Api.Appointments;

public sealed class BookAppointmentHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public BookAppointmentHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Appointment>> HandleAsync(BookAppointmentCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Patients.TryGetValue(command.PatientId, out var patient) || patient.TenantId != context.TenantId)
        {
            return Result<Appointment>.Failure("Patient was not found for this tenant.");
        }

        if (!_store.StaffUsers.TryGetValue(command.PractitionerId, out var practitioner) || practitioner.TenantId != context.TenantId || practitioner.Role != "Doctor")
        {
            return Result<Appointment>.Failure("Doctor was not found for this tenant.");
        }

        var appointment = new Appointment(Guid.NewGuid(), context.TenantId, command.PatientId, command.PractitionerId, command.ScheduledFor, command.Reason.Trim());
        _store.Appointments[appointment.Id] = appointment;

        await _eventBus.PublishAsync(new AppointmentBookedEvent(Guid.NewGuid(), context.TenantId, appointment.Id, command.PatientId, command.PractitionerId, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "AppointmentBooked", nameof(Appointment), appointment.Id.ToString(), cancellationToken);
        return Result<Appointment>.Success(appointment);
    }
}

public sealed class CheckInPatientHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public CheckInPatientHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Appointment>> HandleAsync(Guid appointmentId, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Appointments.TryGetValue(appointmentId, out var appointment) || appointment.TenantId != context.TenantId)
        {
            return Result<Appointment>.Failure("Appointment was not found for this tenant.");
        }

        appointment.CheckIn();
        await _eventBus.PublishAsync(new PatientCheckedInEvent(Guid.NewGuid(), context.TenantId, appointment.Id, appointment.PatientId, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "PatientCheckedIn", nameof(Appointment), appointment.Id.ToString(), cancellationToken);
        return Result<Appointment>.Success(appointment);
    }
}
