using System.Collections.Concurrent;
using EHR.PatientService.Application.Tenants;

namespace EHR.PatientService.Infrastructure.Tenants;

public sealed class InMemoryTenantRegistrationReadModelRepository : ITenantRegistrationReadModelRepository
{
    private readonly ConcurrentDictionary<string, TenantRegistrationReadModel> _tenants = new(StringComparer.OrdinalIgnoreCase);

    public Task UpsertAsync(TenantRegistrationReadModel tenant, CancellationToken cancellationToken)
    {
        _tenants[tenant.TenantId] = tenant;
        return Task.CompletedTask;
    }

    public Task<TenantRegistrationReadModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        _tenants.TryGetValue(tenantId, out var tenant);
        return Task.FromResult(tenant);
    }
}
