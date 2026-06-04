using System.Collections.Concurrent;
using EHR.AppointmentService.Application.Patients;

namespace EHR.AppointmentService.Infrastructure.Patients;

public sealed class InMemoryKnownPatientRepository : IKnownPatientRepository
{
    private readonly ConcurrentDictionary<Guid, KnownPatient> _patients = new();

    public Task UpsertAsync(KnownPatient patient, CancellationToken cancellationToken)
    {
        _patients[patient.PatientId] = patient;
        return Task.CompletedTask;
    }

    public Task<KnownPatient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        _patients.TryGetValue(patientId, out var patient);
        return Task.FromResult(patient);
    }
}
