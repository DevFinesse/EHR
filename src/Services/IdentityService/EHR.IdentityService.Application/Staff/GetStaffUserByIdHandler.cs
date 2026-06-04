using EHR.Cqrs;
using EHR.IdentityService.Domain.Staff;

namespace EHR.IdentityService.Application.Staff;

public sealed class GetStaffUserByIdHandler : IQueryHandler<GetStaffUserByIdQuery, StaffUser?>
{
    private readonly IStaffUserRepository _repository;

    public GetStaffUserByIdHandler(IStaffUserRepository repository)
    {
        _repository = repository;
    }

    public Task<StaffUser?> HandleAsync(GetStaffUserByIdQuery query, CancellationToken cancellationToken) =>
        _repository.GetByIdAsync(query.Id, cancellationToken);
}
