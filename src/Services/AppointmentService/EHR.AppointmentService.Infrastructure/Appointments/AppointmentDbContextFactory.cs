using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class AppointmentDbContextFactory : IDesignTimeDbContextFactory<AppointmentDbContext>
{
    public AppointmentDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.FirstOrDefault()
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__AppointmentDb")
            ?? "Host=localhost;Port=5436;Database=ehr_appointment;Username=ehr;Password=ehr_dev_password";

        var options = new DbContextOptionsBuilder<AppointmentDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppointmentDbContext(options);
    }
}
