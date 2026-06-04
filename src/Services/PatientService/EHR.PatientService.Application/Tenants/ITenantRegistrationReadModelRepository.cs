namespace EHR.PatientService.Application.Tenants;

public interface ITenantRegistrationReadModelRepository
{
    Task UpsertAsync(TenantRegistrationReadModel tenant, CancellationToken cancellationToken);

    Task<TenantRegistrationReadModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken);
}
