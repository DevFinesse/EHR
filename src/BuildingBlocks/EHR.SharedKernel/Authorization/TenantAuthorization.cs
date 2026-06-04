namespace EHR.SharedKernel.Authorization;

public interface ITenantAuthorizationService
{
    void EnsureCanAccessTenant(string tenantId);

    bool CanAccessTenant(string tenantId);
}

public sealed class TenantAuthorizationService : ITenantAuthorizationService
{
    private readonly ICurrentUserContext _currentUser;

    public TenantAuthorizationService(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public void EnsureCanAccessTenant(string tenantId)
    {
        if (!CanAccessTenant(tenantId))
        {
            throw new TenantAccessDeniedException(tenantId);
        }
    }

    public bool CanAccessTenant(string tenantId)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return false;
        }

        if (_currentUser.IsSuperAdmin)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(_currentUser.TenantId)
            && string.Equals(_currentUser.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TenantAccessDeniedException : UnauthorizedAccessException
{
    public string TenantId { get; }

    public TenantAccessDeniedException(string tenantId)
        : base($"Access to tenant '{tenantId}' is denied.")
    {
        TenantId = tenantId;
    }
}
