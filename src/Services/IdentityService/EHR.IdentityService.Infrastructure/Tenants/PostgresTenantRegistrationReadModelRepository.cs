using EHR.IdentityService.Application.Tenants;
using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Tenants;

public sealed class PostgresTenantRegistrationReadModelRepository : ITenantRegistrationReadModelRepository
{
    private readonly DbContextOptions<IdentityDbContext> _options;

    public PostgresTenantRegistrationReadModelRepository(string connectionString)
    {
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;
    }

    public async Task UpsertAsync(TenantRegistrationReadModel tenant, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.TenantRegistrations.SingleOrDefaultAsync(existing => existing.TenantId == tenant.TenantId, cancellationToken);
        if (row is null)
        {
            db.TenantRegistrations.Add(new TenantRegistrationRow
            {
                TenantId = tenant.TenantId,
                HospitalId = tenant.HospitalId,
                Name = tenant.Name,
                RegisteredAt = tenant.RegisteredAt,
                CorrelationId = tenant.CorrelationId
            });
        }
        else
        {
            row.HospitalId = tenant.HospitalId;
            row.Name = tenant.Name;
            row.RegisteredAt = tenant.RegisteredAt;
            row.CorrelationId = tenant.CorrelationId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<TenantRegistrationReadModel?> GetByTenantIdAsync(string tenantId, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.TenantRegistrations.AsNoTracking().SingleOrDefaultAsync(tenant => tenant.TenantId == tenantId.Trim(), cancellationToken);

        return row is null ? null : new TenantRegistrationReadModel(row.TenantId, row.HospitalId, row.Name, row.RegisteredAt, row.CorrelationId);
    }
}
