namespace EHR.IdentityService.Application.Tenants;

public sealed class TenantNotFoundException : InvalidOperationException
{
    public string TenantId { get; }

    public TenantNotFoundException(string tenantId)
        : base($"Tenant '{tenantId}' does not exist.")
    {
        TenantId = tenantId;
    }
}
