using FluentValidation;

namespace EHR.AppointmentService.Application.Appointments;

public sealed class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty().MaximumLength(120);
        RuleFor(command => command.PatientId).NotEmpty();
        RuleFor(command => command.PractitionerId).NotEmpty();
        RuleFor(command => command.ScheduledFor)
            .GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5))
            .WithMessage("Appointment time cannot be in the past.");
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(500);
    }
}
