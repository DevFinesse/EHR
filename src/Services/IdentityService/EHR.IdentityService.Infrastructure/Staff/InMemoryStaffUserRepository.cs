using System.Collections.Concurrent;
using System.Linq;
using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Domain.Staff;
using EHR.Messaging;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class InMemoryStaffUserRepository : IStaffUserRepository
{
    private readonly ConcurrentDictionary<Guid, StaffUser> _staffUsers = new();
    private readonly IEventBus _eventBus;

    public InMemoryStaffUserRepository(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddAsync(StaffUser staffUser, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (_staffUsers.Values.Any(existing => existing.Email == staffUser.Email && existing.Id != staffUser.Id))
        {
            throw new DuplicateStaffUserEmailException(staffUser.Email);
        }

        _staffUsers[staffUser.Id] = staffUser;
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    public Task SaveAsync(StaffUser staffUser, CancellationToken cancellationToken)
    {
        _staffUsers[staffUser.Id] = staffUser;
        return Task.CompletedTask;
    }

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _staffUsers.TryGetValue(id, out var staffUser);
        return Task.FromResult(staffUser);
    }

    public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var staffUser = _staffUsers.Values.FirstOrDefault(user => user.Email == normalized);
        return Task.FromResult(staffUser);
    }
}
