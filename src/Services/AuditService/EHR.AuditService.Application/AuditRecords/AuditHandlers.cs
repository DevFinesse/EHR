using EHR.AuditService.Domain.AuditRecords;
using EHR.Cqrs;
using EHR.Messaging;
using EHR.SharedKernel.Authorization;

namespace EHR.AuditService.Application.AuditRecords;

public sealed class RecordAuditHandler : ICommandHandler<RecordAuditCommand, AuditRecord>
{
    private readonly IAuditRecordRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;
    private readonly IEventBus _eventBus;

    public RecordAuditHandler(IAuditRecordRepository repository, ITenantAuthorizationService tenantAuthorization, IEventBus eventBus)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
        _eventBus = eventBus;
    }

    public async Task<AuditRecord> HandleAsync(RecordAuditCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var record = AuditRecord.Create(tenantId, command.Action, command.ResourceType, command.ResourceId, command.Severity, command.CorrelationId, command.UserId);
        await _repository.AddAsync(record, cancellationToken);
        await _eventBus.PublishAsync(new AuditEvent(record.Id, record.TenantId, record.Action, record.ResourceType, record.ResourceId, record.CorrelationId), cancellationToken);
        return record;
    }
}

public sealed class ListAuditRecordsHandler : IQueryHandler<ListAuditRecordsQuery, IReadOnlyCollection<AuditRecord>>
{
    private readonly IAuditRecordRepository _repository;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public ListAuditRecordsHandler(IAuditRecordRepository repository, ICurrentUserContext currentUser, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _currentUser = currentUser;
        _tenantAuthorization = tenantAuthorization;
    }

    public Task<IReadOnlyCollection<AuditRecord>> HandleAsync(ListAuditRecordsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.IsSuperAdmin)
        {
            return ListAndFilterAsync(query, query.TenantId?.Trim(), cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_currentUser.TenantId))
        {
            throw new TenantAccessDeniedException(query.TenantId ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            _tenantAuthorization.EnsureCanAccessTenant(query.TenantId);
        }

        return ListAndFilterAsync(query, _currentUser.TenantId, cancellationToken);
    }

    private async Task<IReadOnlyCollection<AuditRecord>> ListAndFilterAsync(ListAuditRecordsQuery query, string? tenantId, CancellationToken cancellationToken)
    {
        var records = await _repository.ListAsync(tenantId, cancellationToken);
        var filtered = records.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            filtered = filtered.Where(record => string.Equals(record.Action, query.Action.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            filtered = filtered.Where(record => string.Equals(record.UserId, query.UserId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (query.From is not null)
        {
            filtered = filtered.Where(record => record.Timestamp >= query.From.Value);
        }

        if (query.To is not null)
        {
            filtered = filtered.Where(record => record.Timestamp <= query.To.Value);
        }

        return filtered.Take(Math.Min(Math.Max(query.Limit, 1), 500)).ToArray();
    }
}
