using EHR.Cqrs;
using EHR.Messaging;
using EHR.PatientService.Domain.Patients;
using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;

namespace EHR.PatientService.Application.Patients;

public sealed class RegisterPatientHandler : ICommandHandler<RegisterPatientCommand, Patient>
{
    private readonly IPatientRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public RegisterPatientHandler(IPatientRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Patient> HandleAsync(RegisterPatientCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var patient = Patient.Register(TenantId.From(tenantId), command.FullName, command.DateOfBirth, command.Sex, command.PhoneNumber);
        var integrationEvent = new PatientRegisteredEvent(Guid.NewGuid(), patient.TenantId.Value, patient.Id, patient.MedicalRecordNumber, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(patient, integrationEvent, cancellationToken);
        return patient;
    }
}
