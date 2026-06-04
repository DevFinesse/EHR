using EHR.Cqrs;
using EHR.EncounterService.Domain.Encounters;

namespace EHR.EncounterService.Application.Encounters;

public sealed record GetEncounterByIdQuery(Guid Id) : IQuery<Encounter?>;
