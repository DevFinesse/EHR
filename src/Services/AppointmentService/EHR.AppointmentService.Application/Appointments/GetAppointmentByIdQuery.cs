using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;

namespace EHR.AppointmentService.Application.Appointments;

public sealed record GetAppointmentByIdQuery(Guid Id) : IQuery<Appointment?>;

public sealed record ListAppointmentsQuery(
    string? TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? PractitionerId,
    string? Status,
    int Limit = 50) : IQuery<IReadOnlyCollection<Appointment>>;
