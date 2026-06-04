using System.Collections.Concurrent;
using EHR.Messaging;
using EHR.TenantService.Application.Hospitals;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Infrastructure.Hospitals;

public sealed class InMemoryHospitalRepository : IHospitalRepository
{
    private readonly ConcurrentDictionary<Guid, Hospital> _hospitals = new();
    private readonly IEventBus _eventBus;

    public InMemoryHospitalRepository(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddAsync(Hospital hospital, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _hospitals[hospital.Id] = hospital;
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    public Task<Hospital?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _hospitals.TryGetValue(id, out var hospital);
        return Task.FromResult(hospital);
    }
}
