using EHR.Cqrs;
using EHR.PatientService.Domain.Patients;

namespace EHR.PatientService.Application.Patients;

public sealed record RegisterPatientCommand(string TenantId, string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber) : ICommand<Patient>;
