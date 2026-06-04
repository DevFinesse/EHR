using FluentValidation;

namespace EHR.Api.Staff;

public static class StaffEndpoints
{
    public static IEndpointRouteBuilder MapStaffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/staff").WithTags("Staff");

        group.MapPost("/", async (
            CreateStaffUserCommand command,
            IValidator<CreateStaffUserCommand> validator,
            CreateStaffUserHandler handler,
            TenantContextAccessor context,
            CancellationToken cancellationToken) =>
        {
            var validation = await validator.ValidateAsync(command, cancellationToken);
            if (!validation.IsValid)
            {
                return Results.ValidationProblem(validation.ToDictionary());
            }

            var staff = await handler.HandleAsync(command, context, cancellationToken);
            return Results.Created($"/api/staff/{staff.Id}", staff);
        });

        return app;
    }
}

public sealed class CreateStaffUserValidator : AbstractValidator<CreateStaffUserCommand>
{
    private static readonly string[] Roles =
    [
        "Doctor", "Nurse", "Receptionist", "Lab Technician", "Pharmacist", "Cashier", "Hospital Admin", "Super Admin"
    ];

    public CreateStaffUserValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(command => command.Role).Must(role => Roles.Contains(role)).WithMessage("Role is not supported.");
        RuleFor(command => command.Department).NotEmpty().MaximumLength(80);
    }
}
