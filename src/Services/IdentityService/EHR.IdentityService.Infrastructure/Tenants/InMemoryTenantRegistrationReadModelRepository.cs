using System.Collections.Concurrent;
using EHR.IdentityService.Application.Tenants;

namespace EHR.IdentityService.Infrastructure.Tenants;

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
        _tenants.TryGetValue(tenantId.Trim(), out var tenant);
        return Task.FromResult(tenant);
    }
}
