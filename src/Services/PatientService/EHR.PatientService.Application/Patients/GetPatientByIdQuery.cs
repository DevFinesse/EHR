using EHR.Cqrs;
using EHR.PatientService.Domain.Patients;

namespace EHR.PatientService.Application.Patients;

public sealed record GetPatientByIdQuery(Guid Id) : IQuery<Patient?>;

public sealed record SearchPatientsQuery(
    string? TenantId,
    string? MedicalRecordNumber,
    string? Name,
    string? PhoneNumber,
    int Limit = 50) : IQuery<IReadOnlyCollection<Patient>>;
