using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel;

namespace EHR.AppointmentService.Application.Appointments;

public sealed record BookAppointmentCommand(string TenantId, Guid PatientId, Guid PractitionerId, DateTimeOffset ScheduledFor, string Reason) : ICommand<Result<Appointment>>;
