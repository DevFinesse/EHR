using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;

namespace EHR.AppointmentService.Application.Appointments;

public sealed record GetAppointmentByIdQuery(Guid Id) : IQuery<Appointment?>;
