using EHR.SharedKernel.Authorization;
using FluentValidation;

namespace EHR.Api.Patients;

public static class PatientEndpoints
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/patients").WithTags("Patients");

        group.MapPost("/", async (
            RegisterPatientCommand command,
            IValidator<RegisterPatientCommand> validator,
            RegisterPatientHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var patient = await handler.HandleAsync(command, context, cancellationToken);
            return Results.Created($"/api/patients/{patient.Id}", patient);
        });

        group.MapGet("/{id:guid}", (Guid id, EhrStore store, TenantContextAccessor context) =>
        {
            if (!store.Patients.TryGetValue(id, out var patient))
            {
                return Results.NotFound();
            }

            var current = context.Current;
            return current.Role == PlatformRoles.SuperAdmin || patient.TenantId == current.TenantId
                ? Results.Ok(patient)
                : Results.NotFound();
        });

        return app;
    }
}

public sealed class RegisterPatientValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.DateOfBirth).LessThan(DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Date of birth must be in the past.");
        RuleFor(command => command.Sex).NotEmpty().Must(value => new[] { "Female", "Male", "Other" }.Contains(value)).WithMessage("Sex must be Female, Male, or Other.");
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(30);
    }
}
