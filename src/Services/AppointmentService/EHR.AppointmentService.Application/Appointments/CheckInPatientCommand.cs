using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel;

namespace EHR.AppointmentService.Application.Appointments;

public sealed record CheckInPatientCommand(Guid AppointmentId) : ICommand<Result<Appointment>>;
