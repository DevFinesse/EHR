using EHR.Api.Audit;
using EHR.Messaging;

namespace EHR.Api.Tenants;

public sealed class RegisterHospitalHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public RegisterHospitalHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<Hospital> HandleAsync(RegisterHospitalCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var tenantId = $"tenant-{Guid.NewGuid():N}";
        var hospital = new Hospital(Guid.NewGuid(), tenantId, command.Name.Trim(), command.Country.Trim(), command.City.Trim(), command.Plan.Trim());
        _store.Hospitals[hospital.Id] = hospital;

        await _eventBus.PublishAsync(new HospitalRegisteredEvent(Guid.NewGuid(), tenantId, hospital.Id, hospital.Name, contextAccessor.Current.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(contextAccessor.Current with { TenantId = tenantId }, "HospitalRegistered", nameof(Hospital), hospital.Id.ToString(), cancellationToken);

        return hospital;
    }
}
