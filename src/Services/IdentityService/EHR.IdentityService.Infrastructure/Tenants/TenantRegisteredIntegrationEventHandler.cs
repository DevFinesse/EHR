using System.Text.Json;
using EHR.IdentityService.Application.Tenants;
using EHR.Messaging;

namespace EHR.IdentityService.Infrastructure.Tenants;

public sealed class TenantRegisteredIntegrationEventHandler : IIntegrationEventHandler
{
    private readonly ITenantRegistrationReadModelRepository _repository;

    public TenantRegisteredIntegrationEventHandler(ITenantRegistrationReadModelRepository repository)
    {
        _repository = repository;
    }

    public string EventType => "tenant.hospital.registered";

    public async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(envelope.Payload);
        var hospital = JsonSerializer.Deserialize<HospitalRegisteredPayload>(payload);
        if (hospital is null)
        {
            return;
        }

        var tenant = new TenantRegistrationReadModel(
            envelope.TenantId,
            hospital.HospitalId,
            hospital.Name,
            envelope.OccurredAt,
            envelope.CorrelationId);

        await _repository.UpsertAsync(tenant, cancellationToken);
    }

    private sealed record HospitalRegisteredPayload(Guid HospitalId, string Name);
}
