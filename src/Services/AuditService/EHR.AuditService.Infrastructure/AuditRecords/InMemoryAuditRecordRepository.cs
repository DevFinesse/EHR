using System.Collections.Concurrent;
using EHR.AuditService.Application.AuditRecords;
using EHR.AuditService.Domain.AuditRecords;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public sealed class InMemoryAuditRecordRepository : IAuditRecordRepository
{
    private readonly ConcurrentQueue<AuditRecord> _records = new();

    public Task AddAsync(AuditRecord auditRecord, CancellationToken cancellationToken)
    {
        _records.Enqueue(auditRecord);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AuditRecord>> ListAsync(string? tenantId, CancellationToken cancellationToken)
    {
        var records = _records
            .Where(record => tenantId is null || record.TenantId == tenantId)
            .OrderByDescending(record => record.Timestamp)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AuditRecord>>(records);
    }
}
