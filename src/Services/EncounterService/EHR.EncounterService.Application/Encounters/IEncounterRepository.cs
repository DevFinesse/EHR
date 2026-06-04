using EHR.Messaging;
using EHR.EncounterService.Domain.Encounters;

namespace EHR.EncounterService.Application.Encounters;

public interface IEncounterRepository
{
    Task AddAsync(Encounter encounter, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task SaveAsync(Encounter encounter, IntegrationEvent? integrationEvent, CancellationToken cancellationToken);

    Task<Encounter?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
