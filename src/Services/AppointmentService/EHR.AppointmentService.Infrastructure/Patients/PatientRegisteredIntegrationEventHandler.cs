using System.Text.Json;
using EHR.AppointmentService.Application.Patients;
using EHR.Messaging;

namespace EHR.AppointmentService.Infrastructure.Patients;

public sealed class PatientRegisteredIntegrationEventHandler : IIntegrationEventHandler
{
    private readonly IKnownPatientRepository _repository;

    public PatientRegisteredIntegrationEventHandler(IKnownPatientRepository repository)
    {
        _repository = repository;
    }

    public string EventType => "patient.created";

    public async Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(envelope.Payload);
        var patient = JsonSerializer.Deserialize<PatientRegisteredPayload>(payload);
        if (patient is null)
        {
            return;
        }

        await _repository.UpsertAsync(new KnownPatient(
            patient.PatientId,
            envelope.TenantId,
            patient.MedicalRecordNumber,
            envelope.OccurredAt,
            envelope.CorrelationId), cancellationToken);
    }

    private sealed record PatientRegisteredPayload(Guid PatientId, string MedicalRecordNumber);
}
