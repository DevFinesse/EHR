using Microsoft.EntityFrameworkCore;

namespace EHR.AnalyticsService.Omop;

public static class OmopDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<OmopDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new OmopDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
