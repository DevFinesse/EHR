using System.Collections.Concurrent;
using EHR.Messaging;
using EHR.PatientService.Application.Patients;
using EHR.PatientService.Domain.Patients;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<Guid, Patient> _patients = new();
    private readonly IEventBus _eventBus;

    public InMemoryPatientRepository(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task AddAsync(Patient patient, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        _patients[patient.Id] = patient;
        await _eventBus.PublishAsync(integrationEvent, cancellationToken);
    }

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _patients.TryGetValue(id, out var patient);
        return Task.FromResult(patient);
    }
}
