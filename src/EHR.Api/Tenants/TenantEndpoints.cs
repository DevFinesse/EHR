using FluentValidation;

namespace EHR.Api.Tenants;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/hospitals").WithTags("Tenant");

        group.MapPost("/", async (
            RegisterHospitalCommand command,
            IValidator<RegisterHospitalCommand> validator,
            RegisterHospitalHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var hospital = await handler.HandleAsync(command, context, cancellationToken);
            return Results.Created($"/api/hospitals/{hospital.Id}", hospital);
        });

        return app;
    }
}

public sealed class RegisterHospitalValidator : AbstractValidator<RegisterHospitalCommand>
{
    public RegisterHospitalValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(80);
        RuleFor(command => command.City).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Plan).NotEmpty().MaximumLength(40);
    }
}
