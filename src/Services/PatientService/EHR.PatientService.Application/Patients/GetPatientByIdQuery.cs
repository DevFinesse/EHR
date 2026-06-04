using EHR.Cqrs;
using EHR.PatientService.Domain.Patients;

namespace EHR.PatientService.Application.Patients;

public sealed record GetPatientByIdQuery(Guid Id) : IQuery<Patient?>;
