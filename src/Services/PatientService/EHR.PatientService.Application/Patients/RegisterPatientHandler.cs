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
    private readonly ICurrentUserContext _currentUser;

    public RegisterPatientHandler(IPatientRepository repository, ITenantAuthorizationService tenantAuthorization, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
        _currentUser = currentUser;
    }

    public async Task<Patient> HandleAsync(RegisterPatientCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var patient = Patient.Register(TenantId.From(tenantId), command.FullName, command.DateOfBirth, command.Sex, command.PhoneNumber);
        var integrationEvent = new PatientRegisteredEvent(Guid.NewGuid(), patient.TenantId.Value, patient.Id, patient.MedicalRecordNumber, _currentUser.CorrelationId);
        await _repository.AddAsync(patient, integrationEvent, cancellationToken);
        return patient;
    }
}
