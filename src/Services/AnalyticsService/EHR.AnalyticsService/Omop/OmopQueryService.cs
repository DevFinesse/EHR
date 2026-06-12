using Microsoft.EntityFrameworkCore;

namespace EHR.AnalyticsService.Omop;

public sealed class OmopQueryService
{
    private readonly DbContextOptions<OmopDbContext>? _options;

    public OmopQueryService(string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _options = new DbContextOptionsBuilder<OmopDbContext>().UseNpgsql(connectionString).Options;
        }
    }

    public async Task<IReadOnlyCollection<OmopPersonRow>> PersonsAsync(string? tenantId, int limit, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return [];
        }

        await using var db = new OmopDbContext(_options);
        return await db.Persons.AsNoTracking()
            .Where(row => tenantId == null || row.TenantId == tenantId)
            .OrderBy(row => row.FullName)
            .Take(Clamp(limit))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OmopVisitOccurrenceRow>> VisitsAsync(string? tenantId, Guid? personId, int limit, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return [];
        }

        await using var db = new OmopDbContext(_options);
        return await db.VisitOccurrences.AsNoTracking()
            .Where(row => tenantId == null || row.TenantId == tenantId)
            .Where(row => personId == null || row.PersonId == personId)
            .OrderByDescending(row => row.VisitStartDateTime)
            .Take(Clamp(limit))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OmopConditionOccurrenceRow>> ConditionsAsync(string? tenantId, Guid? personId, int limit, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return [];
        }

        await using var db = new OmopDbContext(_options);
        return await db.ConditionOccurrences.AsNoTracking()
            .Where(row => tenantId == null || row.TenantId == tenantId)
            .Where(row => personId == null || row.PersonId == personId)
            .OrderByDescending(row => row.ConditionStartDateTime)
            .Take(Clamp(limit))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OmopMeasurementRow>> MeasurementsAsync(string? tenantId, Guid? personId, string? sourceValue, int limit, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return [];
        }

        await using var db = new OmopDbContext(_options);
        return await db.Measurements.AsNoTracking()
            .Where(row => tenantId == null || row.TenantId == tenantId)
            .Where(row => personId == null || row.PersonId == personId)
            .Where(row => sourceValue == null || row.MeasurementSourceValue == sourceValue)
            .OrderByDescending(row => row.MeasurementDateTime)
            .Take(Clamp(limit))
            .ToArrayAsync(cancellationToken);
    }

    private static int Clamp(int limit) => Math.Min(Math.Max(limit, 1), 500);
}
