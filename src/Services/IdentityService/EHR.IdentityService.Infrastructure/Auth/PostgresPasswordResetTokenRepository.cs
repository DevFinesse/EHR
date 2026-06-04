using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Auth;

public sealed class PostgresPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly DbContextOptions<IdentityDbContext> _options;

    public PostgresPasswordResetTokenRepository(string connectionString) =>
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;

    public async Task StoreAsync(PasswordResetTokenRecord resetToken, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        db.PasswordResetTokens.Add(new PasswordResetTokenRow { Token = resetToken.Token, StaffUserId = resetToken.StaffUserId, ExpiresAt = resetToken.ExpiresAt, UsedAt = resetToken.UsedAt });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PasswordResetTokenRecord?> GetAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.PasswordResetTokens.AsNoTracking().SingleOrDefaultAsync(resetToken => resetToken.Token == token, cancellationToken);
        return row is null ? null : new PasswordResetTokenRecord(row.Token, row.StaffUserId, row.ExpiresAt, row.UsedAt);
    }

    public async Task MarkUsedAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.PasswordResetTokens.SingleOrDefaultAsync(resetToken => resetToken.Token == token, cancellationToken);
        if (row is not null)
        {
            row.UsedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
