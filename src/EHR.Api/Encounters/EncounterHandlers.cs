using EHR.Api.Audit;
using EHR.Messaging;
using EHR.SharedKernel;

namespace EHR.Api.Encounters;

public sealed class StartEncounterHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public StartEncounterHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Encounter>> HandleAsync(StartEncounterCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Appointments.TryGetValue(command.AppointmentId, out var appointment) || appointment.TenantId != context.TenantId)
        {
            return Result<Encounter>.Failure("Checked-in appointment was not found for this tenant.");
        }

        if (appointment.Status != "CheckedIn")
        {
            return Result<Encounter>.Failure("Patient must be checked in before an encounter can start.");
        }

        var encounter = new Encounter(Guid.NewGuid(), context.TenantId, appointment.Id, appointment.PatientId, appointment.PractitionerId, command.VisitType.Trim());
        _store.Encounters[encounter.Id] = encounter;

        await _eventBus.PublishAsync(new EncounterStartedEvent(Guid.NewGuid(), context.TenantId, encounter.Id, encounter.PatientId, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "EncounterStarted", nameof(Encounter), encounter.Id.ToString(), cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}

public sealed class RecordVitalsHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public RecordVitalsHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Encounter>> HandleAsync(Guid encounterId, RecordVitalsCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Encounters.TryGetValue(encounterId, out var encounter) || encounter.TenantId != context.TenantId)
        {
            return Result<Encounter>.Failure("Encounter was not found for this tenant.");
        }

        encounter.RecordVitals(new VitalSigns(command.TemperatureCelsius, command.SystolicBloodPressure, command.DiastolicBloodPressure, command.PulseRate, command.OxygenSaturation));
        await _eventBus.PublishAsync(new VitalsRecordedEvent(Guid.NewGuid(), context.TenantId, encounter.Id, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "VitalsRecorded", nameof(Encounter), encounter.Id.ToString(), cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}

public sealed class AddDiagnosisHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public AddDiagnosisHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Encounter>> HandleAsync(Guid encounterId, AddDiagnosisCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Encounters.TryGetValue(encounterId, out var encounter) || encounter.TenantId != context.TenantId)
        {
            return Result<Encounter>.Failure("Encounter was not found for this tenant.");
        }

        encounter.AddDiagnosis(new Diagnosis(command.Code.Trim().ToUpperInvariant(), command.Description.Trim(), command.Certainty.Trim()));
        await _eventBus.PublishAsync(new DiagnosisAddedEvent(Guid.NewGuid(), context.TenantId, encounter.Id, command.Code.Trim().ToUpperInvariant(), context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "DiagnosisAdded", nameof(Encounter), encounter.Id.ToString(), cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}

public sealed class CompleteEncounterHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public CompleteEncounterHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Result<Encounter>> HandleAsync(Guid encounterId, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        if (!_store.Encounters.TryGetValue(encounterId, out var encounter) || encounter.TenantId != context.TenantId)
        {
            return Result<Encounter>.Failure("Encounter was not found for this tenant.");
        }

        if (!encounter.Vitals.Any())
        {
            return Result<Encounter>.Failure("At least one vitals record is required before completion.");
        }

        if (!encounter.Diagnoses.Any())
        {
            return Result<Encounter>.Failure("At least one diagnosis is required before completion.");
        }

        encounter.Complete();
        await _eventBus.PublishAsync(new EncounterCompletedEvent(Guid.NewGuid(), context.TenantId, encounter.Id, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "EncounterCompleted", nameof(Encounter), encounter.Id.ToString(), cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}
