namespace EHR.AppointmentService.Domain.Appointments;

public sealed class Appointment
{
    private Appointment(Guid id, string tenantId, Guid patientId, Guid practitionerId, DateTimeOffset scheduledFor, string reason)
    {
        Id = id;
        TenantId = tenantId;
        PatientId = patientId;
        PractitionerId = practitionerId;
        ScheduledFor = scheduledFor;
        Reason = reason;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid PatientId { get; }
    public Guid PractitionerId { get; }
    public DateTimeOffset ScheduledFor { get; }
    public string Reason { get; }
    public string Status { get; private set; } = "Booked";

    public static Appointment Book(string tenantId, Guid patientId, Guid practitionerId, DateTimeOffset scheduledFor, string reason) =>
        new(Guid.NewGuid(), tenantId.Trim(), patientId, practitionerId, scheduledFor, reason.Trim());

    public static Appointment Restore(Guid id, string tenantId, Guid patientId, Guid practitionerId, DateTimeOffset scheduledFor, string reason, string status)
    {
        var appointment = new Appointment(id, tenantId, patientId, practitionerId, scheduledFor, reason);
        appointment.Status = status;
        return appointment;
    }

    public void CheckIn() => Status = "CheckedIn";
}
