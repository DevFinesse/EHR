using EHR.Messaging;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public interface IHospitalRepository
{
    Task AddAsync(Hospital hospital, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task<Hospital?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
