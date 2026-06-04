using EHR.AuditService.Application.AuditRecords;
using EHR.AuditService.Domain.AuditRecords;
using Microsoft.EntityFrameworkCore;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public sealed class PostgresAuditRecordRepository : IAuditRecordRepository
{
    private readonly DbContextOptions<AuditDbContext> _options;

    public PostgresAuditRecordRepository(string connectionString) =>
        _options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(connectionString).Options;

    public async Task AddAsync(AuditRecord auditRecord, CancellationToken cancellationToken)
    {
        await using var db = new AuditDbContext(_options);
        db.AuditRecords.Add(new AuditRecordRow
        {
            Id = auditRecord.Id,
            TenantId = auditRecord.TenantId,
            Action = auditRecord.Action,
            ResourceType = auditRecord.ResourceType,
            ResourceId = auditRecord.ResourceId,
            Severity = auditRecord.Severity,
            Timestamp = auditRecord.Timestamp,
            CorrelationId = auditRecord.CorrelationId,
            UserId = auditRecord.UserId
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuditRecord>> ListAsync(string? tenantId, CancellationToken cancellationToken)
    {
        await using var db = new AuditDbContext(_options);
        var query = db.AuditRecords.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            query = query.Where(record => record.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(record => record.Timestamp)
            .Select(record => AuditRecord.Restore(record.Id, record.TenantId, record.Action, record.ResourceType, record.ResourceId, record.Severity, record.CorrelationId, record.UserId))
            .ToArrayAsync(cancellationToken);
    }
}
