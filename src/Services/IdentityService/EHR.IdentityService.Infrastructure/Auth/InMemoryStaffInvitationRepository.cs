using System.Collections.Concurrent;
using EHR.IdentityService.Application.Auth;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class InMemoryStaffInvitationRepository : IStaffInvitationRepository
{
    private readonly ConcurrentDictionary<string, StaffInvitation> _invitations = new();

    public Task StoreAsync(StaffInvitation invitation, CancellationToken cancellationToken)
    {
        _invitations[invitation.Token] = invitation;
        return Task.CompletedTask;
    }

    public Task<StaffInvitation?> GetAsync(string token, CancellationToken cancellationToken)
    {
        _invitations.TryGetValue(token, out var invitation);
        return Task.FromResult(invitation);
    }

    public Task MarkAcceptedAsync(string token, CancellationToken cancellationToken)
    {
        if (_invitations.TryGetValue(token, out var invitation))
        {
            _invitations[token] = invitation with { AcceptedAt = DateTimeOffset.UtcNow };
        }

        return Task.CompletedTask;
    }
}
