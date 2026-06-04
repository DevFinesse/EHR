using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class EncounterDbContext : DbContext
{
    public EncounterDbContext(DbContextOptions<EncounterDbContext> options) : base(options)
    {
    }

    public DbSet<EncounterRow> Encounters => Set<EncounterRow>();
    public DbSet<EncounterOutboxMessageRow> OutboxMessages => Set<EncounterOutboxMessageRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncounterRow>(entity =>
        {
            entity.ToTable("encounters");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.TenantId);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.AppointmentId).HasColumnName("appointment_id");
            entity.Property(row => row.PatientId).HasColumnName("patient_id");
            entity.Property(row => row.PractitionerId).HasColumnName("practitioner_id");
            entity.Property(row => row.VisitType).HasColumnName("visit_type");
            entity.Property(row => row.Status).HasColumnName("status");
            entity.Property(row => row.VitalsJson).HasColumnName("vitals").HasColumnType("jsonb");
            entity.Property(row => row.DiagnosesJson).HasColumnName("diagnoses").HasColumnType("jsonb");
        });

        modelBuilder.Entity<EncounterOutboxMessageRow>(entity =>
        {
            entity.MapOutboxMessage();
        });
    }
}

public sealed class EncounterRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public Guid PractitionerId { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VitalsJson { get; set; } = "[]";
    public string DiagnosesJson { get; set; } = "[]";
}

public sealed class EncounterOutboxMessageRow : OutboxMessageRowBase
{
    public static EncounterOutboxMessageRow FromIntegrationEvent(IntegrationEvent integrationEvent)
    {
        var row = new EncounterOutboxMessageRow();
        row.Apply(integrationEvent);
        return row;
    }
}
