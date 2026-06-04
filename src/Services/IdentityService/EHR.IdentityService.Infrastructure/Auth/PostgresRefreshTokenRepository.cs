using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class PostgresRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbContextOptions<IdentityDbContext> _options;

    public PostgresRefreshTokenRepository(string connectionString) =>
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;

    public async Task StoreAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        db.RefreshTokens.Add(new RefreshTokenRow { Token = refreshToken.Token, StaffUserId = refreshToken.StaffUserId, ExpiresAt = refreshToken.ExpiresAt, RevokedAt = refreshToken.RevokedAt });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshTokenRecord?> GetAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);
        return row is null ? null : new RefreshTokenRecord(row.Token, row.StaffUserId, row.ExpiresAt, row.RevokedAt);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.RefreshTokens.SingleOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);
        if (row is not null)
        {
            row.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
