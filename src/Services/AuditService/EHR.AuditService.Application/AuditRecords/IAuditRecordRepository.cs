using EHR.AuditService.Domain.AuditRecords;

namespace EHR.AuditService.Application.AuditRecords;

public interface IAuditRecordRepository
{
    Task AddAsync(AuditRecord auditRecord, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AuditRecord>> ListAsync(string? tenantId, CancellationToken cancellationToken);
}
