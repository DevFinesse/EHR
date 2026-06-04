using Microsoft.EntityFrameworkCore;

namespace EHR.PatientService.Infrastructure.Patients;

public static class PatientDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new PatientDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
