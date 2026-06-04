namespace EHR.Messaging;

public sealed record HospitalRegisteredEvent(Guid EventId, string TenantId, Guid HospitalId, string Name, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "tenant.hospital.registered", DateTimeOffset.UtcNow, CorrelationId);

public sealed record StaffUserCreatedEvent(Guid EventId, string TenantId, Guid StaffUserId, string Role, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "identity.staff.created", DateTimeOffset.UtcNow, CorrelationId);

public sealed record PatientRegisteredEvent(Guid EventId, string TenantId, Guid PatientId, string MedicalRecordNumber, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "patient.created", DateTimeOffset.UtcNow, CorrelationId);

public sealed record AppointmentBookedEvent(Guid EventId, string TenantId, Guid AppointmentId, Guid PatientId, Guid PractitionerId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "appointment.booked", DateTimeOffset.UtcNow, CorrelationId);

public sealed record PatientCheckedInEvent(Guid EventId, string TenantId, Guid AppointmentId, Guid PatientId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "patient.checked_in", DateTimeOffset.UtcNow, CorrelationId);

public sealed record EncounterStartedEvent(Guid EventId, string TenantId, Guid EncounterId, Guid PatientId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "encounter.started", DateTimeOffset.UtcNow, CorrelationId);

public sealed record VitalsRecordedEvent(Guid EventId, string TenantId, Guid EncounterId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "vitals.recorded", DateTimeOffset.UtcNow, CorrelationId);

public sealed record DiagnosisAddedEvent(Guid EventId, string TenantId, Guid EncounterId, string Code, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "diagnosis.added", DateTimeOffset.UtcNow, CorrelationId);

public sealed record EncounterCompletedEvent(Guid EventId, string TenantId, Guid EncounterId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "encounter.completed", DateTimeOffset.UtcNow, CorrelationId);

public sealed record AuditEvent(Guid EventId, string TenantId, string Action, string ResourceType, string ResourceId, string CorrelationId)
    : IntegrationEvent(EventId, TenantId, "audit.event", DateTimeOffset.UtcNow, CorrelationId);
