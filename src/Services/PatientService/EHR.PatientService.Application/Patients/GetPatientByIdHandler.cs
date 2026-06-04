using EHR.Cqrs;
using EHR.PatientService.Domain.Patients;
using EHR.SharedKernel.Authorization;

namespace EHR.PatientService.Application.Patients;

public sealed class GetPatientByIdHandler : IQueryHandler<GetPatientByIdQuery, Patient?>
{
    private readonly IPatientRepository _repository;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public GetPatientByIdHandler(IPatientRepository repository, ITenantAuthorizationService tenantAuthorization)
    {
        _repository = repository;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Patient?> HandleAsync(GetPatientByIdQuery query, CancellationToken cancellationToken)
    {
        var patient = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (patient is not null)
        {
            _tenantAuthorization.EnsureCanAccessTenant(patient.TenantId.Value);
        }

        return patient;
    }
}
