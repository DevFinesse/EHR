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
              and (@Action is null or action = @Action)
              and (@UserId is null or user_id = @UserId)
              and (@From is null or timestamp >= @From)
              and (@To is null or timestamp <= @To)
            order by timestamp desc
            limit @Limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AuditRecordReadRow>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            Action = string.IsNullOrWhiteSpace(query.Action) ? null : query.Action.Trim(),
            UserId = string.IsNullOrWhiteSpace(query.UserId) ? null : query.UserId.Trim(),
            query.From,
            query.To,
            Limit = Clamp(query.Limit, 1, 500)
        }, cancellationToken: cancellationToken));
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

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private sealed record AuditRecordReadRow(Guid Id, string TenantId, string Action, string ResourceType, string ResourceId, string Severity, string CorrelationId, string? UserId)
    {
        public AuditRecordReadRow() : this(default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null) { }
    }
}
