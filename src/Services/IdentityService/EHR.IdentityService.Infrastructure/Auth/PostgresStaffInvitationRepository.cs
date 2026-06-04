using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class PostgresStaffInvitationRepository : IStaffInvitationRepository
{
    private readonly DbContextOptions<IdentityDbContext> _options;

    public PostgresStaffInvitationRepository(string connectionString) =>
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;

    public async Task StoreAsync(StaffInvitation invitation, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        db.StaffInvitations.Add(new StaffInvitationRow { Token = invitation.Token, StaffUserId = invitation.StaffUserId, ExpiresAt = invitation.ExpiresAt, AcceptedAt = invitation.AcceptedAt });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffInvitation?> GetAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.StaffInvitations.AsNoTracking().SingleOrDefaultAsync(invitation => invitation.Token == token, cancellationToken);
        return row is null ? null : new StaffInvitation(row.Token, row.StaffUserId, row.ExpiresAt, row.AcceptedAt);
    }

    public async Task MarkAcceptedAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.StaffInvitations.SingleOrDefaultAsync(invitation => invitation.Token == token, cancellationToken);
        if (row is not null)
        {
            row.AcceptedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
