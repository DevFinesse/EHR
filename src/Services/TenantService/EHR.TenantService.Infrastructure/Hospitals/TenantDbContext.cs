using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.TenantService.Infrastructure.Hospitals;

public sealed class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
    {
    }

    public DbSet<HospitalRow> Hospitals => Set<HospitalRow>();
    public DbSet<TenantOutboxMessageRow> OutboxMessages => Set<TenantOutboxMessageRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HospitalRow>(entity =>
        {
            entity.ToTable("hospitals");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.TenantId).IsUnique();
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.Name).HasColumnName("name");
            entity.Property(row => row.Country).HasColumnName("country");
            entity.Property(row => row.City).HasColumnName("city");
            entity.Property(row => row.Plan).HasColumnName("plan");
            entity.Property(row => row.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<TenantOutboxMessageRow>(entity =>
        {
            entity.MapOutboxMessage();
        });
    }
}

public sealed class HospitalRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TenantOutboxMessageRow : OutboxMessageRowBase
{
    public static TenantOutboxMessageRow FromIntegrationEvent(IntegrationEvent integrationEvent)
    {
        var row = new TenantOutboxMessageRow();
        row.Apply(integrationEvent);
        return row;
    }
}
