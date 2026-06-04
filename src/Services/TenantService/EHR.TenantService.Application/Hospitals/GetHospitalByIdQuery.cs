using EHR.Cqrs;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public sealed record GetHospitalByIdQuery(Guid Id) : IQuery<Hospital?>;
