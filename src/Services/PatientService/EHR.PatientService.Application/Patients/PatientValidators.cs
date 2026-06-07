using FluentValidation;

namespace EHR.PatientService.Application.Patients;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(160);
        RuleFor(command => command.DateOfBirth)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .GreaterThan(new DateOnly(1900, 1, 1));
        RuleFor(command => command.Sex).NotEmpty().MaximumLength(40);
        RuleFor(command => command.PhoneNumber).NotEmpty().MaximumLength(40);
    }
}
