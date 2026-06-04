using EHR.Cqrs;
using EHR.TenantService.Domain.Hospitals;

namespace EHR.TenantService.Application.Hospitals;

public sealed class GetHospitalByIdHandler : IQueryHandler<GetHospitalByIdQuery, Hospital?>
{
    private readonly IHospitalRepository _repository;

    public GetHospitalByIdHandler(IHospitalRepository repository)
    {
        _repository = repository;
    }

    public Task<Hospital?> HandleAsync(GetHospitalByIdQuery query, CancellationToken cancellationToken) =>
        _repository.GetByIdAsync(query.Id, cancellationToken);
}
