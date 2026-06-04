using Microsoft.EntityFrameworkCore;

namespace EHR.TenantService.Infrastructure.Hospitals;

public static class TenantDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new TenantDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
