namespace EHR.AppointmentService.Application.Patients;

public interface IKnownPatientRepository
{
    Task UpsertAsync(KnownPatient patient, CancellationToken cancellationToken);

    Task<KnownPatient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken);
}
