using Microsoft.EntityFrameworkCore;

namespace EHR.AuditService.Infrastructure.AuditRecords;

public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditRecordRow> AuditRecords => Set<AuditRecordRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditRecordRow>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(row => row.Id);
            entity.HasIndex(row => row.TenantId);
            entity.Property(row => row.Id).HasColumnName("id");
            entity.Property(row => row.TenantId).HasColumnName("tenant_id");
            entity.Property(row => row.Action).HasColumnName("action");
            entity.Property(row => row.ResourceType).HasColumnName("resource_type");
            entity.Property(row => row.ResourceId).HasColumnName("resource_id");
            entity.Property(row => row.Severity).HasColumnName("severity");
            entity.Property(row => row.Timestamp).HasColumnName("timestamp");
            entity.Property(row => row.CorrelationId).HasColumnName("correlation_id");
            entity.Property(row => row.UserId).HasColumnName("user_id");
        });
    }
}

public sealed class AuditRecordRow
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? UserId { get; set; }
}
