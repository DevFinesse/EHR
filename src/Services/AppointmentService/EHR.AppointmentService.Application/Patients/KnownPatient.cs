namespace EHR.AppointmentService.Application.Patients;

public sealed record KnownPatient(
    Guid PatientId,
    string TenantId,
    string MedicalRecordNumber,
    DateTimeOffset RegisteredAt,
    string CorrelationId);
