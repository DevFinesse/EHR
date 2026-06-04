using EHR.Cqrs;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public sealed record RegisterHospitalCommand(string Name, string Country, string City, string Plan) : ICommand<Hospital>;
