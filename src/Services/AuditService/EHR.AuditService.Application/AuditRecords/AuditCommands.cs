using EHR.AuditService.Domain.AuditRecords;
using EHR.Cqrs;

namespace EHR.AuditService.Application.AuditRecords;

public sealed record RecordAuditCommand(string TenantId, string Action, string ResourceType, string ResourceId, string Severity, string CorrelationId, string? UserId) : ICommand<AuditRecord>;

public sealed record ListAuditRecordsQuery(
    string? TenantId,
    string? Action = null,
    string? UserId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 100) : IQuery<IReadOnlyCollection<AuditRecord>>;
