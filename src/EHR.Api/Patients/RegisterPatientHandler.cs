using EHR.Api.Audit;
using EHR.Messaging;

namespace EHR.Api.Patients;

public sealed class RegisterPatientHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public RegisterPatientHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Patient> HandleAsync(RegisterPatientCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        var patient = new Patient(
            Guid.NewGuid(),
            context.TenantId,
            $"MRN-{DateTimeOffset.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}",
            command.FullName.Trim(),
            command.DateOfBirth,
            command.Sex.Trim(),
            command.PhoneNumber.Trim());

        _store.Patients[patient.Id] = patient;

        await _eventBus.PublishAsync(new PatientRegisteredEvent(Guid.NewGuid(), context.TenantId, patient.Id, patient.MedicalRecordNumber, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "PatientRegistered", nameof(Patient), patient.Id.ToString(), cancellationToken);
        return patient;
    }
}
