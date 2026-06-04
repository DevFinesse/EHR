using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class EncounterDbContextFactory : IDesignTimeDbContextFactory<EncounterDbContext>
{
    public EncounterDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__EncounterDb")
            ?? "Host=localhost;Port=5437;Database=ehr_encounter;Username=ehr;Password=ehr_dev_password";

        var options = new DbContextOptionsBuilder<EncounterDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new EncounterDbContext(options);
    }
}
