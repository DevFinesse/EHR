using FluentValidation;

namespace EHR.Api.Encounters;

public static class EncounterEndpoints
{
    public static IEndpointRouteBuilder MapEncounterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/encounters").WithTags("Encounters");

        group.MapPost("/", async (
            StartEncounterCommand command,
            IValidator<StartEncounterCommand> validator,
            StartEncounterHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, context, cancellationToken);
            return result.IsSuccess ? Results.Created($"/api/encounters/{result.Value!.Id}", result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/vitals", async (
            Guid id,
            RecordVitalsCommand command,
            IValidator<RecordVitalsCommand> validator,
            RecordVitalsHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(id, command, context, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/diagnoses", async (
            Guid id,
            AddDiagnosisCommand command,
            IValidator<AddDiagnosisCommand> validator,
            AddDiagnosisHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(id, command, context, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            CompleteEncounterHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, context, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}

public sealed class StartEncounterValidator : AbstractValidator<StartEncounterCommand>
{
    public StartEncounterValidator()
    {
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.VisitType).NotEmpty().MaximumLength(60);
    }
}

public sealed class RecordVitalsValidator : AbstractValidator<RecordVitalsCommand>
{
    public RecordVitalsValidator()
    {
        RuleFor(command => command.TemperatureCelsius).InclusiveBetween(30, 45);
        RuleFor(command => command.SystolicBloodPressure).InclusiveBetween(60, 260);
        RuleFor(command => command.DiastolicBloodPressure).InclusiveBetween(30, 180);
        RuleFor(command => command.PulseRate).InclusiveBetween(20, 240);
        RuleFor(command => command.OxygenSaturation).InclusiveBetween(50, 100);
    }
}

public sealed class AddDiagnosisValidator : AbstractValidator<AddDiagnosisCommand>
{
    public AddDiagnosisValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(20);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(240);
        RuleFor(command => command.Certainty).NotEmpty().Must(value => new[] { "Suspected", "Confirmed", "RuledOut" }.Contains(value)).WithMessage("Certainty must be Suspected, Confirmed, or RuledOut.");
    }
}
