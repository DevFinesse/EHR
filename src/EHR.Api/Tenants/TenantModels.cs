using EHR.SharedKernel;

namespace EHR.Api.Tenants;

public sealed class Hospital : Entity
{
    public Hospital(Guid id, string tenantId, string name, string country, string city, string plan)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Country = country;
        City = city;
        Plan = plan;
    }

    public string TenantId { get; }
    public string Name { get; }
    public string Country { get; }
    public string City { get; }
    public string Plan { get; }
}

public sealed record RegisterHospitalCommand(string Name, string Country, string City, string Plan);
