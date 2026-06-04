namespace EHR.SharedKernel;

public interface ITenantScopedEntity
{
    string TenantId { get; set; }
}
