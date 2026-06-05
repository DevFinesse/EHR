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
    private readonly IStaffMetadataRepository _staffMetadata;

    public CreateStaffUserHandler(IStaffUserRepository repository, ITenantRegistrationReadModelRepository tenants, ITenantAuthorizationService tenantAuthorization, IStaffMetadataRepository staffMetadata)
    {
        _repository = repository;
        _tenants = tenants;
        _tenantAuthorization = tenantAuthorization;
        _staffMetadata = staffMetadata;
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

        var role = await _staffMetadata.NormalizeRoleAsync(tenantId, command.Role, cancellationToken);
        if (role is null)
        {
            throw new ArgumentException($"Unsupported staff role '{command.Role}'.", nameof(command.Role));
        }

        var department = await _staffMetadata.NormalizeDepartmentAsync(tenantId, command.Department, cancellationToken);
        if (department is null)
        {
            throw new ArgumentException($"Unsupported staff department '{command.Department}'.", nameof(command.Department));
        }

        var staffUser = StaffUser.Create(tenantId, command.FullName, command.Email, role, department);
        var integrationEvent = new StaffUserCreatedEvent(Guid.NewGuid(), staffUser.TenantId, staffUser.Id, staffUser.Role, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(staffUser, integrationEvent, cancellationToken);
        return staffUser;
    }
}
