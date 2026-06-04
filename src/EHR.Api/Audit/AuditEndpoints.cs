namespace EHR.Api.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit-events").WithTags("Audit");

        group.MapGet("/", (EhrStore store) =>
            Results.Ok(store.AuditRecords.OrderByDescending(record => record.Timestamp)));

        return app;
    }
}
