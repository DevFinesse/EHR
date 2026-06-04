using Microsoft.EntityFrameworkCore;

namespace EHR.EncounterService.Infrastructure.Encounters;

public static class EncounterDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<EncounterDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new EncounterDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
