using EHR.Cqrs;
using EHR.Messaging;
using EHR.SharedKernel.Authorization;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public sealed class RegisterHospitalHandler : ICommandHandler<RegisterHospitalCommand, Hospital>
{
    private readonly IHospitalRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public RegisterHospitalHandler(IHospitalRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<Hospital> HandleAsync(RegisterHospitalCommand command, CancellationToken cancellationToken)
    {
        var hospital = Hospital.Register(command.Name, command.Country, command.City, command.Plan);
        var integrationEvent = new HospitalRegisteredEvent(Guid.NewGuid(), hospital.TenantId, hospital.Id, hospital.Name, _currentUser.CorrelationId);
        await _repository.AddAsync(hospital, integrationEvent, cancellationToken);
        return hospital;
    }
}
