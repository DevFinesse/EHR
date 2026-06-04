using System.Collections.Concurrent;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.Messaging;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class InMemoryEncounterRepository : IEncounterRepository
{
    private readonly ConcurrentDictionary<Guid, Encounter> _encounters = new();
    private readonly IEventBus _eventBus;

    public InMemoryEncounterRepository(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddAsync(Encounter encounter, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _encounters[encounter.Id] = encounter;
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    public async Task SaveAsync(Encounter encounter, IntegrationEvent? integrationEvent, CancellationToken cancellationToken)
    {
        _encounters[encounter.Id] = encounter;
        if (integrationEvent is not null)
        {
            await _eventBus.PublishAsync(integrationEvent, cancellationToken);
        }
    }

    public Task<Encounter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _encounters.TryGetValue(id, out var encounter);
        return Task.FromResult(encounter);
    }
}
