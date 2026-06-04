using EHR.Cqrs;
using EHR.EncounterService.Domain.Encounters;
using EHR.Messaging;
using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;

namespace EHR.EncounterService.Application.Encounters;

public sealed class StartEncounterHandler : ICommandHandler<StartEncounterCommand, Encounter>
{
    private readonly IEncounterRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public StartEncounterHandler(IEncounterRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Encounter> HandleAsync(StartEncounterCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var encounter = Encounter.Start(tenantId, command.AppointmentId, command.PatientId, command.PractitionerId, command.VisitType);
        var integrationEvent = new EncounterStartedEvent(Guid.NewGuid(), encounter.TenantId, encounter.Id, encounter.PatientId, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(encounter, integrationEvent, cancellationToken);
        return encounter;
    }
}

public sealed class RecordVitalsHandler : ICommandHandler<RecordVitalsCommand, Result<Encounter>>
{
    private readonly IEncounterRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public RecordVitalsHandler(IEncounterRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Result<Encounter>> HandleAsync(RecordVitalsCommand command, CancellationToken cancellationToken)
    {
        var encounter = await _repository.GetByIdAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return Result<Encounter>.Failure("Encounter was not found.");
        }

        _tenantAuthorization.EnsureCanAccessTenant(encounter.TenantId);

        encounter.RecordVitals(new VitalSigns(command.TemperatureCelsius, command.SystolicBloodPressure, command.DiastolicBloodPressure, command.PulseRate, command.OxygenSaturation));
        var integrationEvent = new VitalsRecordedEvent(Guid.NewGuid(), encounter.TenantId, encounter.Id, Guid.NewGuid().ToString("N"));
        await _repository.SaveAsync(encounter, integrationEvent, cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}

public sealed class AddDiagnosisHandler : ICommandHandler<AddDiagnosisCommand, Result<Encounter>>
{
    private readonly IEncounterRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public AddDiagnosisHandler(IEncounterRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Result<Encounter>> HandleAsync(AddDiagnosisCommand command, CancellationToken cancellationToken)
    {
        var encounter = await _repository.GetByIdAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return Result<Encounter>.Failure("Encounter was not found.");
        }

        _tenantAuthorization.EnsureCanAccessTenant(encounter.TenantId);

        var code = command.Code.Trim().ToUpperInvariant();
        encounter.AddDiagnosis(new Diagnosis(code, command.Description.Trim(), command.Certainty.Trim()));
        var integrationEvent = new DiagnosisAddedEvent(Guid.NewGuid(), encounter.TenantId, encounter.Id, code, Guid.NewGuid().ToString("N"));
        await _repository.SaveAsync(encounter, integrationEvent, cancellationToken);
        return Result<Encounter>.Success(encounter);
    }
}

public sealed class CompleteEncounterHandler : ICommandHandler<CompleteEncounterCommand, Result<Encounter>>
{
    private readonly IEncounterRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public CompleteEncounterHandler(IEncounterRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Result<Encounter>> HandleAsync(CompleteEncounterCommand command, CancellationToken cancellationToken)
    {
        var encounter = await _repository.GetByIdAsync(command.EncounterId, cancellationToken);
        if (encounter is null)
        {
            return Result<Encounter>.Failure("Encounter was not found.");
        }

        _tenantAuthorization.EnsureCanAccessTenant(encounter.TenantId);

        var completion = encounter.Complete();
        if (!completion.IsSuccess)
        {
            return completion;
        }

        var integrationEvent = new EncounterCompletedEvent(Guid.NewGuid(), encounter.TenantId, encounter.Id, Guid.NewGuid().ToString("N"));
        await _repository.SaveAsync(encounter, integrationEvent, cancellationToken);
        return completion;
    }
}

public sealed class GetEncounterByIdHandler : IQueryHandler<GetEncounterByIdQuery, Encounter?>
{
    private readonly IEncounterRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public GetEncounterByIdHandler(IEncounterRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Encounter?> HandleAsync(GetEncounterByIdQuery query, CancellationToken cancellationToken)
    {
        var encounter = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (encounter is not null)
        {
            _tenantAuthorization.EnsureCanAccessTenant(encounter.TenantId);
        }

        return encounter;
    }
}
