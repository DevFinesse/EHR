using Microsoft.EntityFrameworkCore;

namespace EHR.AnalyticsService.Omop;

public sealed class OmopConceptMapService
{
    private readonly DbContextOptions<OmopDbContext>? _options;

    public OmopConceptMapService(string? connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _options = new DbContextOptionsBuilder<OmopDbContext>().UseNpgsql(connectionString).Options;
        }
    }

    public async Task<IReadOnlyCollection<OmopConceptMapRow>> ListAsync(string? domain, string? sourceVocabulary, string? sourceCode, int limit, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return [];
        }

        await using var db = new OmopDbContext(_options);
        return await db.ConceptMaps.AsNoTracking()
            .Where(row => domain == null || row.Domain == domain)
            .Where(row => sourceVocabulary == null || row.SourceVocabulary == sourceVocabulary)
            .Where(row => sourceCode == null || row.SourceCode == sourceCode)
            .OrderBy(row => row.Domain)
            .ThenBy(row => row.SourceVocabulary)
            .ThenBy(row => row.SourceCode)
            .Take(Math.Min(Math.Max(limit, 1), 500))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<OmopConceptMapRow?> FindAsync(string domain, string sourceVocabulary, string sourceCode, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            return null;
        }

        await using var db = new OmopDbContext(_options);
        return await db.ConceptMaps.AsNoTracking().SingleOrDefaultAsync(row =>
            row.Domain == domain &&
            row.SourceVocabulary == sourceVocabulary &&
            row.SourceCode == sourceCode,
            cancellationToken);
    }

    public async Task<OmopConceptMapRow> UpsertAsync(UpsertConceptMapRequest request, CancellationToken cancellationToken)
    {
        if (_options is null)
        {
            throw new InvalidOperationException("AnalyticsDb is not configured.");
        }

        await using var db = new OmopDbContext(_options);
        var domain = Normalize(request.Domain);
        var sourceVocabulary = Normalize(request.SourceVocabulary);
        var sourceCode = request.SourceCode.Trim();
        var row = await db.ConceptMaps.SingleOrDefaultAsync(existing =>
            existing.Domain == domain &&
            existing.SourceVocabulary == sourceVocabulary &&
            existing.SourceCode == sourceCode,
            cancellationToken);

        if (row is null)
        {
            row = new OmopConceptMapRow
            {
                Id = Guid.NewGuid(),
                Domain = domain,
                SourceVocabulary = sourceVocabulary,
                SourceCode = sourceCode,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.ConceptMaps.Add(row);
        }

        row.SourceName = request.SourceName?.Trim();
        row.SourceConceptId = request.SourceConceptId;
        row.StandardVocabulary = Normalize(request.StandardVocabulary);
        row.StandardConceptId = request.StandardConceptId;
        row.StandardConceptCode = request.StandardConceptCode.Trim();
        row.StandardConceptName = request.StandardConceptName.Trim();
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return row;
    }

    private static string Normalize(string value) => value.Trim();
}

public sealed record UpsertConceptMapRequest(
    string Domain,
    string SourceVocabulary,
    string SourceCode,
    string? SourceName,
    long SourceConceptId,
    string StandardVocabulary,
    long StandardConceptId,
    string StandardConceptCode,
    string StandardConceptName);
