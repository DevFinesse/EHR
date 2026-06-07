using FluentValidation;

namespace EHR.IdentityService.Application.Staff;

public sealed class CreateStaffUserCommandValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Role).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Department).NotEmpty().MaximumLength(120);
    }
}
