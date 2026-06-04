namespace EHR.SharedKernel;

public sealed record TenantContext(
    string TenantId,
    string? BranchId,
    string? UserId,
    string? Role,
    string CorrelationId)
{
    public static TenantContext Platform(string correlationId) =>
        new("platform", null, null, "System", correlationId);
}
