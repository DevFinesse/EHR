using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;
using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class PatientDbContext : DbContext
{
    private readonly ICurrentUserContext? _currentUser;

    public PatientDbContext(DbContextOptions<PatientDbContext> options) : base(options)
    {
    }

    public PatientDbContext(DbContextOptions<PatientDbContext> options, ICurrentUserContext currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<PatientRow> Patients => Set<PatientRow>();
    public DbSet<TenantRegistrationRow> TenantRegistrations => Set<TenantRegistrationRow>();
    public DbSet<OutboxMessageRow> OutboxMessages => Set<OutboxMessageRow>();

    public override int SaveChanges()
    {
        StampTenantScopedEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenantScopedEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientRow>(entity =>
        {
            entity.ToTable("patients");
            entity.HasKey(row => row.Id);
            entity.HasQueryFilter(row => _currentUser == null || _currentUser.IsSuperAdmin || row.TenantId == _currentUser.TenantId);
            entity.HasIndex(row => row.TenantId);
            entity.HasIndex(row => row.MedicalRecordNumber).IsUnique();
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.MedicalRecordNumber).HasColumnName("medical_record_number");
            entity.Property(row => row.FullName).HasColumnName("full_name");
            entity.Property(row => row.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(row => row.Sex).HasColumnName("sex");
            entity.Property(row => row.PhoneNumber).HasColumnName("phone_number");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<TenantRegistrationRow>(entity =>
        {
            entity.ToTable("tenant_registrations");
            entity.HasKey(row => row.TenantId);
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.HospitalId).HasColumnName("hospital_id");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.RegisteredAt).HasColumnName("registered_at");
            entity.Property(row => row.CorrelationId).HasColumnName("correlation_id");
        });

        modelBuilder.Entity<OutboxMessageRow>(entity =>
        {
            entity.MapOutboxMessage();
        });
    }

    private void StampTenantScopedEntities()
    {
        if (_currentUser is null || !_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.TenantId))
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<ITenantScopedEntity>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            StampTenant(entry);
        }
    }

    private void StampTenant(EntityEntry<ITenantScopedEntity> entry)
    {
        if (_currentUser is { IsSuperAdmin: true } && !string.IsNullOrWhiteSpace(entry.Entity.TenantId))
        {
            return;
        }

        entry.Entity.TenantId = _currentUser!.TenantId!;
    }
}

public sealed class PatientRow : ITenantScopedEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TenantRegistrationRow
{
    public string TenantId { get; set; } = string.Empty;
    public Guid HospitalId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset RegisteredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

public sealed class OutboxMessageRow : OutboxMessageRowBase
{
    public static OutboxMessageRow FromIntegrationEvent(IntegrationEvent integrationEvent)
    {
        var row = new OutboxMessageRow();
        row.Apply(integrationEvent);
        return row;
    }
}
