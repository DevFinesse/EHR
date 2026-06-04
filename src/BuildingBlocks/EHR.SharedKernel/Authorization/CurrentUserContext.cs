using System.Security.Claims;

namespace EHR.SharedKernel.Authorization;

public interface ICurrentUserContext
{
    string? UserId { get; }

    string? TenantId { get; }

    string? Role { get; }

    IReadOnlyCollection<string> Permissions { get; }

    bool IsAuthenticated { get; }

    bool IsSuperAdmin { get; }

    bool HasPermission(string permission);
}

public sealed class AnonymousCurrentUserContext : ICurrentUserContext
{
    public string? UserId => null;

    public string? TenantId => null;

    public string? Role => null;

    public IReadOnlyCollection<string> Permissions => [];

    public bool IsAuthenticated => false;

    public bool IsSuperAdmin => false;

    public bool HasPermission(string permission) => false;
}

public sealed class ClaimsPrincipalCurrentUserContext : ICurrentUserContext
{
    private readonly ClaimsPrincipal _principal;

    public ClaimsPrincipalCurrentUserContext(ClaimsPrincipal principal)
    {
        _principal = principal;
    }

    public string? UserId => FindFirstValue(ClaimTypes.NameIdentifier) ?? FindFirstValue("sub");

    public string? TenantId => FindFirstValue("tenant_id");

    public string? Role => FindFirstValue(ClaimTypes.Role);

    public IReadOnlyCollection<string> Permissions => _principal.FindAll("permission").Select(claim => claim.Value).ToArray();

    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin => string.Equals(Role, PlatformRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase);

    public bool HasPermission(string permission) =>
        Permissions.Any(value => string.Equals(value, permission, StringComparison.OrdinalIgnoreCase));

    private string? FindFirstValue(string claimType) => _principal.FindFirst(claimType)?.Value;
}
