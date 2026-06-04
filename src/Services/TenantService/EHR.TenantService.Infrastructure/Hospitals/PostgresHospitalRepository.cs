using EHR.Messaging;
using EHR.TenantService.Application.Hospitals;
using EHR.TenantService.Domain.Hospitals;
using Microsoft.EntityFrameworkCore;

namespace EHR.TenantService.Infrastructure.Hospitals;

public sealed class PostgresHospitalRepository : IHospitalRepository
{
    private readonly DbContextOptions<TenantDbContext> _options;

    public PostgresHospitalRepository(string connectionString)
    {
        _options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public async Task AddAsync(Hospital hospital, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = new TenantDbContext(_options);
        if (await db.Hospitals.AnyAsync(row => row.Id == hospital.Id, cancellationToken))
        {
            return;
        }

        db.Hospitals.Add(new HospitalRow
        {
            Id = hospital.Id,
            TenantId = hospital.TenantId,
            Name = hospital.Name,
            Country = hospital.Country,
            City = hospital.City,
            Plan = hospital.Plan,
            CreatedAt = hospital.CreatedAt
        });

        db.OutboxMessages.Add(TenantOutboxMessageRow.FromIntegrationEvent(integrationEvent));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Hospital?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = new TenantDbContext(_options);
        var row = await db.Hospitals.AsNoTracking().SingleOrDefaultAsync(hospital => hospital.Id == id, cancellationToken);

        return row is null ? null : Hospital.Restore(row.Id, row.TenantId, row.Name, row.Country, row.City, row.Plan);
    }
}
