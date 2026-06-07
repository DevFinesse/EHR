using FluentValidation;

namespace EHR.IdentityService.Application.Auth;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Password).NotEmpty();
        RuleFor(command => command.MfaCode).MaximumLength(20);
    }
}

public sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}

public sealed class InviteStaffCommandValidator : AbstractValidator<InviteStaffCommand>
{
    public InviteStaffCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(160);
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(command => command.Role).NotEmpty().MaximumLength(80);
        RuleFor(command => command.Department).NotEmpty().MaximumLength(120);
    }
}

public sealed class AcceptStaffInvitationCommandValidator : AbstractValidator<AcceptStaffInvitationCommand>
{
    public AcceptStaffInvitationCommandValidator()
    {
        RuleFor(command => command.InvitationToken).NotEmpty();
        RuleFor(command => command.Password).SetValidator(new PasswordValidator());
        RuleFor(command => command.MfaCode).MaximumLength(20);
    }
}

public sealed class EnableMfaCommandValidator : AbstractValidator<EnableMfaCommand>
{
    public EnableMfaCommandValidator()
    {
        RuleFor(command => command.StaffUserId).NotEmpty();
    }
}

public sealed class SetupMfaCommandValidator : AbstractValidator<SetupMfaCommand>
{
    public SetupMfaCommandValidator()
    {
        RuleFor(command => command.StaffUserId).NotEmpty();
    }
}

public sealed class ResetPasswordRequestCommandValidator : AbstractValidator<ResetPasswordRequestCommand>
{
    public ResetPasswordRequestCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }
}

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.ResetToken).NotEmpty();
        RuleFor(command => command.NewPassword).SetValidator(new PasswordValidator());
    }
}

internal sealed class PasswordValidator : AbstractValidator<string>
{
    public PasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty()
            .MinimumLength(12)
            .Must(password => password.Any(char.IsUpper)).WithMessage("Password must contain an uppercase letter.")
            .Must(password => password.Any(char.IsLower)).WithMessage("Password must contain a lowercase letter.")
            .Must(password => password.Any(char.IsDigit)).WithMessage("Password must contain a digit.")
            .Must(password => password.Any(character => !char.IsLetterOrDigit(character))).WithMessage("Password must contain a symbol.");
    }
}
