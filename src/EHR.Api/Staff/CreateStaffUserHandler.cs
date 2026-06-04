using EHR.Api.Audit;
using EHR.Messaging;

namespace EHR.Api.Staff;

public sealed class CreateStaffUserHandler
{
    private readonly EhrStore _store;
    private readonly IEventBus _eventBus;
    private readonly AuditTrail _auditTrail;

    public CreateStaffUserHandler(EhrStore store, IEventBus eventBus, AuditTrail auditTrail)
    {
        _store = store;
        _eventBus = eventBus;
        _auditTrail = auditTrail;
    }

    public async Task<StaffUser> HandleAsync(CreateStaffUserCommand command, TenantContextAccessor contextAccessor, CancellationToken cancellationToken)
    {
        var context = contextAccessor.Current;
        var staff = new StaffUser(Guid.NewGuid(), context.TenantId, command.FullName.Trim(), command.Email.Trim().ToLowerInvariant(), command.Role.Trim(), command.Department.Trim());
        _store.StaffUsers[staff.Id] = staff;

        await _eventBus.PublishAsync(new StaffUserCreatedEvent(Guid.NewGuid(), context.TenantId, staff.Id, staff.Role, context.CorrelationId), cancellationToken);
        await _auditTrail.RecordAsync(context, "StaffUserCreated", nameof(StaffUser), staff.Id.ToString(), cancellationToken);
        return staff;
    }
}
