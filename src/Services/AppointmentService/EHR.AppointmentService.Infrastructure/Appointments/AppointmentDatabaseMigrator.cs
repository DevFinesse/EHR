using Microsoft.EntityFrameworkCore;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public static class AppointmentDatabaseMigrator
{
    public static async Task MigrateAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<AppointmentDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new AppointmentDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
