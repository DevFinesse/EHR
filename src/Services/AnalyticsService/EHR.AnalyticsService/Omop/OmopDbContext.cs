using Microsoft.EntityFrameworkCore;

namespace EHR.AnalyticsService.Omop;

public sealed class OmopDbContext : DbContext
{
    public OmopDbContext(DbContextOptions<OmopDbContext> options) : base(options)
    {
    }

    public DbSet<OmopPersonRow> Persons => Set<OmopPersonRow>();
    public DbSet<OmopVisitOccurrenceRow> VisitOccurrences => Set<OmopVisitOccurrenceRow>();
    public DbSet<OmopConditionOccurrenceRow> ConditionOccurrences => Set<OmopConditionOccurrenceRow>();
    public DbSet<OmopMeasurementRow> Measurements => Set<OmopMeasurementRow>();
    public DbSet<OmopConceptMapRow> ConceptMaps => Set<OmopConceptMapRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OmopPersonRow>(entity =>
        {
            entity.ToTable("omop_person");
            entity.HasKey(row => row.PersonId);
            entity.HasIndex(row => new { row.TenantId, row.SourcePatientId }).IsUnique();
            entity.Property(row => row.PersonId).HasColumnName("person_id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.SourcePatientId).HasColumnName("source_patient_id");
            entity.Property(row => row.PersonSourceValue).HasColumnName("person_source_value");
            entity.Property(row => row.GenderSourceValue).HasColumnName("gender_source_value");
            entity.Property(row => row.BirthDate).HasColumnName("birth_date");
            entity.Property(row => row.FullName).HasColumnName("full_name");
            entity.Property(row => row.PhoneNumber).HasColumnName("phone_number");
            entity.Property(row => row.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<OmopVisitOccurrenceRow>(entity =>
        {
            entity.ToTable("omop_visit_occurrence");
            entity.HasKey(row => row.VisitOccurrenceId);
            entity.HasIndex(row => new { row.TenantId, row.SourceEncounterId }).IsUnique();
            entity.HasIndex(row => row.PersonId);
            entity.Property(row => row.VisitOccurrenceId).HasColumnName("visit_occurrence_id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.PersonId).HasColumnName("person_id");
            entity.Property(row => row.SourceEncounterId).HasColumnName("source_encounter_id");
            entity.Property(row => row.SourceAppointmentId).HasColumnName("source_appointment_id");
            entity.Property(row => row.ProviderId).HasColumnName("provider_id");
            entity.Property(row => row.VisitSourceValue).HasColumnName("visit_source_value");
            entity.Property(row => row.VisitStartDateTime).HasColumnName("visit_start_datetime");
            entity.Property(row => row.VisitEndDateTime).HasColumnName("visit_end_datetime");
        });

        modelBuilder.Entity<OmopConditionOccurrenceRow>(entity =>
        {
            entity.ToTable("omop_condition_occurrence");
            entity.HasKey(row => row.ConditionOccurrenceId);
            entity.HasIndex(row => row.VisitOccurrenceId);
            entity.Property(row => row.ConditionOccurrenceId).HasColumnName("condition_occurrence_id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.PersonId).HasColumnName("person_id");
            entity.Property(row => row.VisitOccurrenceId).HasColumnName("visit_occurrence_id");
            entity.Property(row => row.ConditionSourceValue).HasColumnName("condition_source_value");
            entity.Property(row => row.ConditionConceptId).HasColumnName("condition_concept_id");
            entity.Property(row => row.ConditionSourceConceptId).HasColumnName("condition_source_concept_id");
            entity.Property(row => row.ConditionSourceText).HasColumnName("condition_source_text");
            entity.Property(row => row.ConditionStatusSourceValue).HasColumnName("condition_status_source_value");
            entity.Property(row => row.ConditionStartDateTime).HasColumnName("condition_start_datetime");
            entity.Property(row => row.SourceEventId).HasColumnName("source_event_id");
        });

        modelBuilder.Entity<OmopMeasurementRow>(entity =>
        {
            entity.ToTable("omop_measurement");
            entity.HasKey(row => row.MeasurementId);
            entity.HasIndex(row => row.VisitOccurrenceId);
            entity.Property(row => row.MeasurementId).HasColumnName("measurement_id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.PersonId).HasColumnName("person_id");
            entity.Property(row => row.VisitOccurrenceId).HasColumnName("visit_occurrence_id");
            entity.Property(row => row.MeasurementSourceValue).HasColumnName("measurement_source_value");
            entity.Property(row => row.MeasurementConceptId).HasColumnName("measurement_concept_id");
            entity.Property(row => row.MeasurementSourceConceptId).HasColumnName("measurement_source_concept_id");
            entity.Property(row => row.ValueAsNumber).HasColumnName("value_as_number");
            entity.Property(row => row.UnitSourceValue).HasColumnName("unit_source_value");
            entity.Property(row => row.UnitConceptId).HasColumnName("unit_concept_id");
            entity.Property(row => row.UnitSourceConceptId).HasColumnName("unit_source_concept_id");
            entity.Property(row => row.MeasurementDateTime).HasColumnName("measurement_datetime");
            entity.Property(row => row.SourceEventId).HasColumnName("source_event_id");
        });

        modelBuilder.Entity<OmopConceptMapRow>(entity =>
        {
            entity.ToTable("omop_concept_map");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => new { row.Domain, row.SourceVocabulary, row.SourceCode }).IsUnique();
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.Domain).HasColumnName("domain");
            entity.Property(row => row.SourceVocabulary).HasColumnName("source_vocabulary");
            entity.Property(row => row.SourceCode).HasColumnName("source_code");
            entity.Property(row => row.SourceName).HasColumnName("source_name");
            entity.Property(row => row.SourceConceptId).HasColumnName("source_concept_id");
            entity.Property(row => row.StandardVocabulary).HasColumnName("standard_vocabulary");
            entity.Property(row => row.StandardConceptId).HasColumnName("standard_concept_id");
            entity.Property(row => row.StandardConceptCode).HasColumnName("standard_concept_code");
            entity.Property(row => row.StandardConceptName).HasColumnName("standard_concept_name");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
            entity.Property(row => row.UpdatedAt).HasColumnName("updated_at");
        });
    }
}

public sealed class OmopPersonRow
{
    public Guid PersonId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid SourcePatientId { get; set; }
    public string PersonSourceValue { get; set; } = string.Empty;
    public string? GenderSourceValue { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class OmopVisitOccurrenceRow
{
    public Guid VisitOccurrenceId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid SourceEncounterId { get; set; }
    public Guid? SourceAppointmentId { get; set; }
    public Guid? ProviderId { get; set; }
    public string? VisitSourceValue { get; set; }
    public DateTimeOffset VisitStartDateTime { get; set; }
    public DateTimeOffset? VisitEndDateTime { get; set; }
}

public sealed class OmopConditionOccurrenceRow
{
    public Guid ConditionOccurrenceId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid VisitOccurrenceId { get; set; }
    public string ConditionSourceValue { get; set; } = string.Empty;
    public long ConditionConceptId { get; set; }
    public long ConditionSourceConceptId { get; set; }
    public string? ConditionSourceText { get; set; }
    public string? ConditionStatusSourceValue { get; set; }
    public DateTimeOffset ConditionStartDateTime { get; set; }
    public Guid SourceEventId { get; set; }
}

public sealed class OmopMeasurementRow
{
    public Guid MeasurementId { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid VisitOccurrenceId { get; set; }
    public string MeasurementSourceValue { get; set; } = string.Empty;
    public long MeasurementConceptId { get; set; }
    public long MeasurementSourceConceptId { get; set; }
    public decimal ValueAsNumber { get; set; }
    public string UnitSourceValue { get; set; } = string.Empty;
    public long UnitConceptId { get; set; }
    public long UnitSourceConceptId { get; set; }
    public DateTimeOffset MeasurementDateTime { get; set; }
    public Guid SourceEventId { get; set; }
}

public sealed class OmopConceptMapRow
{
    public Guid Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string SourceVocabulary { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;
    public string? SourceName { get; set; }
    public long SourceConceptId { get; set; }
    public string StandardVocabulary { get; set; } = string.Empty;
    public long StandardConceptId { get; set; }
    public string StandardConceptCode { get; set; } = string.Empty;
    public string StandardConceptName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
