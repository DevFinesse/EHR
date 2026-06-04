using System.Security.Claims;
using EHR.SharedKernel;

namespace EHR.Api;

public sealed class TenantContextAccessor
{
    public TenantContext Current { get; set; } = TenantContext.Platform(Guid.NewGuid().ToString("N"));

    public static TenantContext FromHttpContext(HttpContext context)
    {
        var correlationId = Read(context.Request.Headers, "X-Correlation-Id") ?? Guid.NewGuid().ToString("N");
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return new TenantContext(
                context.User.FindFirst("tenant_id")?.Value ?? "platform",
                Read(context.Request.Headers, "X-Branch-Id"),
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value,
                context.User.FindFirst(ClaimTypes.Role)?.Value,
                correlationId);
        }

        return FromHeaders(context.Request.Headers);
    }

    public static TenantContext FromHeaders(IHeaderDictionary headers)
    {
        var correlationId = Read(headers, "X-Correlation-Id") ?? Guid.NewGuid().ToString("N");
        return new TenantContext(
            Read(headers, "X-Tenant-Id") ?? "platform",
            Read(headers, "X-Branch-Id"),
            Read(headers, "X-User-Id"),
            Read(headers, "X-Role"),
            correlationId);
    }

    private static string? Read(IHeaderDictionary headers, string key) =>
        headers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : null;
}
