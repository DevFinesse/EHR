using EHR.Messaging;
using EHR.IdentityService.Domain.Staff;

namespace EHR.IdentityService.Application.Staff;

public interface IStaffUserRepository
{
    Task AddAsync(StaffUser staffUser, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task SaveAsync(StaffUser staffUser, CancellationToken cancellationToken);

    Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}
