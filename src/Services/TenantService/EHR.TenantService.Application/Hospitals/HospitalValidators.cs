using FluentValidation;

namespace EHR.TenantService.Application.Hospitals;

public sealed class RegisterHospitalCommandValidator : AbstractValidator<RegisterHospitalCommand>
{
    public RegisterHospitalCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Country).NotEmpty().MaximumLength(100);
        RuleFor(command => command.City).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Plan).NotEmpty().MaximumLength(80);
    }
}
