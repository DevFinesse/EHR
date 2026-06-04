using Microsoft.EntityFrameworkCore;

namespace EHR.IdentityService.Infrastructure.Staff;

public static class IdentityDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new IdentityDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
