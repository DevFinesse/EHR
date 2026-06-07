using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Http;

namespace EHR.ServiceDefaults;

public sealed class HttpCurrentUserContextAccessor : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ICurrentUserContext Current =>
        _httpContextAccessor.HttpContext?.User is { } user
            ? new ClaimsPrincipalCurrentUserContext(user)
            : new AnonymousCurrentUserContext();

    public string? UserId => Current.UserId;

    public string? TenantId => Current.TenantId;

    public string? Role => Current.Role;

    public IReadOnlyCollection<string> Permissions => Current.Permissions;

    public string CorrelationId =>
        _httpContextAccessor.HttpContext is { } context
            ? System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
            : Current.CorrelationId;

    public bool IsAuthenticated => Current.IsAuthenticated;

    public bool IsSuperAdmin => Current.IsSuperAdmin;

    public bool HasPermission(string permission) => Current.HasPermission(permission);
}
