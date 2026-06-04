using EHR.SharedKernel;

namespace EHR.Api.Appointments;

public sealed class Appointment : Entity
{
    public Appointment(Guid id, string tenantId, Guid patientId, Guid practitionerId, DateTimeOffset scheduledFor, string reason)
        : base(id)
    {
        TenantId = tenantId;
        PatientId = patientId;
        PractitionerId = practitionerId;
        ScheduledFor = scheduledFor;
        Reason = reason;
    }

    public string TenantId { get; }
    public Guid PatientId { get; }
    public Guid PractitionerId { get; }
    public DateTimeOffset ScheduledFor { get; }
    public string Reason { get; }
    public string Status { get; private set; } = "Booked";

    public void CheckIn() => Status = "CheckedIn";
}

public sealed record BookAppointmentCommand(Guid PatientId, Guid PractitionerId, DateTimeOffset ScheduledFor, string Reason);
