using EHR.Cqrs;
using EHR.EncounterService.Domain.Encounters;

namespace EHR.EncounterService.Application.Encounters;

public sealed record GetEncounterByIdQuery(Guid Id) : IQuery<Encounter?>;

public sealed record ListEncountersQuery(
    string? TenantId,
    Guid? PatientId,
    string? Status,
    int Limit = 50) : IQuery<IReadOnlyCollection<Encounter>>;
