namespace EHR.TenantService.Domain.Hospitals;

public sealed class Hospital
{
    private Hospital(Guid id, string tenantId, string name, string country, string city, string plan)
    {
        Id = id;
        TenantId = tenantId;
        Name = name;
        Country = country;
        City = city;
        Plan = plan;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string Name { get; }
    public string Country { get; }
    public string City { get; }
    public string Plan { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Hospital Register(string name, string country, string city, string plan) =>
        new(Guid.NewGuid(), $"tenant-{Guid.NewGuid():N}", name.Trim(), country.Trim(), city.Trim(), plan.Trim());

    public static Hospital Restore(Guid id, string tenantId, string name, string country, string city, string plan) =>
        new(id, tenantId, name, country, city, plan);
}
