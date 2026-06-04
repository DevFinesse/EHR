using FluentValidation;

namespace EHR.Api.Appointments;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments").WithTags("Appointments");

        group.MapPost("/", async (
            BookAppointmentCommand command,
            IValidator<BookAppointmentCommand> validator,
            BookAppointmentHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var result = await handler.HandleAsync(command, context, cancellationToken);
            return result.IsSuccess ? Results.Created($"/api/appointments/{result.Value!.Id}", result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/{id:guid}/check-in", async (
            Guid id,
            CheckInPatientHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, context, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(new { error = result.Error });
        });

        return app;
    }
}

public sealed class BookAppointmentValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentValidator()
    {
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.PractitionerId).NotEmpty();
        RuleFor(command => command.ScheduledFor).GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5));
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(240);
    }
}
