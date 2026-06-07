using Dapper;
using EHR.AuditService.Application.AuditRecords;
using EHR.AuditService.Domain.AuditRecords;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public sealed class DapperAuditRecordsQueryHandler : IQueryHandler<ListAuditRecordsQuery, IReadOnlyCollection<AuditRecord>>
{
    private readonly string _connectionString;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperAuditRecordsQueryHandler(string connectionString, ICurrentUserContext currentUser, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _currentUser = currentUser;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<IReadOnlyCollection<AuditRecord>> HandleAsync(ListAuditRecordsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantFilter(query.TenantId);
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   action,
                   resource_type as ResourceType,
                   resource_id as ResourceId,
                   severity,
                   correlation_id as CorrelationId,
                   user_id as UserId
            from audit_records
            where (@TenantId is null or tenant_id = @TenantId)
            order by timestamp desc
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AuditRecordReadRow>(new CommandDefinition(sql, new { TenantId = tenantId }, cancellationToken: cancellationToken));
        return rows.Select(row => AuditRecord.Restore(row.Id, row.TenantId, row.Action, row.ResourceType, row.ResourceId, row.Severity, row.CorrelationId, row.UserId)).ToArray();
    }

    private string? ResolveTenantFilter(string? requestedTenantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            return string.IsNullOrWhiteSpace(requestedTenantId) ? null : requestedTenantId.Trim();
        }

        if (string.IsNullOrWhiteSpace(_currentUser.TenantId))
        {
            throw new TenantAccessDeniedException(requestedTenantId ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(requestedTenantId))
        {
            _tenantAuthorization.EnsureCanAccessTenant(requestedTenantId);
        }

        return _currentUser.TenantId;
    }

    private sealed record AuditRecordReadRow(Guid Id, string TenantId, string Action, string ResourceType, string ResourceId, string Severity, string CorrelationId, string? UserId);
}
