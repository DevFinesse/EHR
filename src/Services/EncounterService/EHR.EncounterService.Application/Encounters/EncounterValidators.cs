using FluentValidation;

namespace EHR.EncounterService.Application.Encounters;

public sealed class StartEncounterCommandValidator : AbstractValidator<StartEncounterCommand>
{
    public StartEncounterCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.AppointmentId).NotEmpty();
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.PractitionerId).NotEmpty();
        RuleFor(command => command.VisitType).NotEmpty().MaximumLength(80);
    }
}
