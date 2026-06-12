namespace EHR.Hl7Api;

public sealed record Hl7InboundMessage(
    string RawMessage,
    string MessageType,
    string TriggerEvent,
    string ControlId,
    string SendingApplication,
    string SendingFacility,
    string ReceivingApplication,
    string ReceivingFacility,
    string Version,
    Hl7Patient Patient,
    Hl7Visit? Visit);

public sealed record Hl7Patient(
    string? ExternalPatientId,
    string? MedicalRecordNumber,
    string? AssigningAuthority,
    string? FamilyName,
    string? GivenName,
    DateOnly? DateOfBirth,
    string? Sex,
    string? PhoneNumber,
    string? Address);

public sealed record Hl7Visit(
    string? PatientClass,
    string? AssignedLocation,
    string? AttendingDoctorId,
    string? AttendingDoctorName,
    string? VisitNumber,
    DateTimeOffset? AdmitDateTime);

public sealed record Hl7AckResponse(
    string Status,
    string MessageType,
    string TriggerEvent,
    string ControlId,
    string AckMessage);

public sealed record BuildAdtMessageRequest(
    string TriggerEvent,
    string SendingApplication,
    string SendingFacility,
    string ReceivingApplication,
    string ReceivingFacility,
    string MessageControlId,
    Hl7Patient Patient,
    Hl7Visit? Visit);

public sealed record BuildAdtMessageResponse(
    string MessageType,
    string TriggerEvent,
    string MessageControlId,
    string Hl7Message);
