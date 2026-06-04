using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class AppointmentDbContext : DbContext
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options)
    {
    }

    public DbSet<AppointmentRow> Appointments => Set<AppointmentRow>();
    public DbSet<KnownPatientRow> KnownPatients => Set<KnownPatientRow>();
    public DbSet<AppointmentOutboxMessageRow> OutboxMessages => Set<AppointmentOutboxMessageRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppointmentRow>(entity =>
        {
            entity.ToTable("appointments");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.TenantId);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.PatientId).HasColumnName("patient_id");
            entity.Property(row => row.PractitionerId).HasColumnName("practitioner_id");
            entity.Property(row => row.ScheduledFor).HasColumnName("scheduled_for");
            entity.Property(row => row.Reason).HasColumnName("reason");
            entity.Property(row => row.Status).HasColumnName("status");
        });

        modelBuilder.Entity<KnownPatientRow>(entity =>
        {
            entity.ToTable("known_patients");
            entity.HasKey(row => row.PatientId);
            entity.HasIndex(row => row.TenantId);
            entity.Property(row => row.PatientId).HasColumnName("patient_id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.MedicalRecordNumber).HasColumnName("medical_record_number");
            entity.Property(row => row.RegisteredAt).HasColumnName("registered_at");
            entity.Property(row => row.CorrelationId).HasColumnName("correlation_id");
        });

        modelBuilder.Entity<AppointmentOutboxMessageRow>(entity =>
        {
            entity.MapOutboxMessage();
        });
    }
}

public sealed class AppointmentRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid PractitionerId { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class KnownPatientRow
{
    public Guid PatientId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class AppointmentOutboxMessageRow : OutboxMessageRowBase
{
    public static AppointmentOutboxMessageRow FromIntegrationEvent(IntegrationEvent integrationEvent)
    {
        var row = new AppointmentOutboxMessageRow();
        row.Apply(integrationEvent);
        return row;
    }
}
