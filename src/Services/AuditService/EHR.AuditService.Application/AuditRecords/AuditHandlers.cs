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
            return _repository.ListAsync(query.TenantId?.Trim(), cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_currentUser.TenantId))
        {
            throw new TenantAccessDeniedException(query.TenantId ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(query.TenantId))
        {
            _tenantAuthorization.EnsureCanAccessTenant(query.TenantId);
        }

        return _repository.ListAsync(_currentUser.TenantId, cancellationToken);
    }
}
