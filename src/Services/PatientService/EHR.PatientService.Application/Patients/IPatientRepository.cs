using EHR.Messaging;
using EHR.PatientService.Domain.Patients;

namespace EHR.PatientService.Application.Patients;

public interface IPatientRepository
{
    Task AddAsync(Patient patient, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task UpdateAsync(Patient patient, IntegrationEvent integrationEvent, CancellationToken cancellationToken);

    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
