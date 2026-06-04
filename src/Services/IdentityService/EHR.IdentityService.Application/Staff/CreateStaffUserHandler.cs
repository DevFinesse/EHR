using EHR.Cqrs;
using EHR.IdentityService.Domain.Staff;
using EHR.IdentityService.Application.Tenants;
using EHR.Messaging;
using EHR.SharedKernel.Authorization;

namespace EHR.IdentityService.Application.Staff;

public sealed class CreateStaffUserHandler : ICommandHandler<CreateStaffUserCommand, StaffUser>
{
    private readonly IStaffUserRepository _repository;
    private readonly ITenantRegistrationReadModelRepository _tenants;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public CreateStaffUserHandler(IStaffUserRepository repository, ITenantRegistrationReadModelRepository tenants, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenants = tenants;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<StaffUser> HandleAsync(CreateStaffUserCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var tenant = await _tenants.GetByTenantIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantNotFoundException(command.TenantId);
        }

        var staffUser = StaffUser.Create(tenantId, command.FullName, command.Email, command.Role, command.Department);
        var integrationEvent = new StaffUserCreatedEvent(Guid.NewGuid(), staffUser.TenantId, staffUser.Id, staffUser.Role, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(staffUser, integrationEvent, cancellationToken);
        return staffUser;
    }
}
