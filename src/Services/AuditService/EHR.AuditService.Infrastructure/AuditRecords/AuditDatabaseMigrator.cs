using Microsoft.EntityFrameworkCore;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public static class AuditDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new AuditDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
