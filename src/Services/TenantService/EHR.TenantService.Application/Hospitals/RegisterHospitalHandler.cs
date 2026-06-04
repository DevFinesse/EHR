using EHR.Cqrs;
using EHR.Messaging;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public sealed class RegisterHospitalHandler : ICommandHandler<RegisterHospitalCommand, Hospital>
{
    private readonly IHospitalRepository _repository;

    public RegisterHospitalHandler(IHospitalRepository repository)
    {
        _repository = repository;
    }

    public async Task<Hospital> HandleAsync(RegisterHospitalCommand command, CancellationToken cancellationToken)
    {
        var hospital = Hospital.Register(command.Name, command.Country, command.City, command.Plan);
        var integrationEvent = new HospitalRegisteredEvent(Guid.NewGuid(), hospital.TenantId, hospital.Id, hospital.Name, Guid.NewGuid().ToString("N"));
        await _repository.AddAsync(hospital, integrationEvent, cancellationToken);
        return hospital;
    }
}
