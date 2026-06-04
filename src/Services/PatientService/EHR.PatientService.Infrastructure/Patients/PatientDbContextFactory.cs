using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class PatientDbContextFactory : IDesignTimeDbContextFactory<PatientDbContext>
{
    public PatientDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__PatientDb")
            ?? "Host=localhost;Port=5435;Database=ehr_patient;Username=ehr;Password=ehr_dev_password";

        var options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new PatientDbContext(options);
    }
}
