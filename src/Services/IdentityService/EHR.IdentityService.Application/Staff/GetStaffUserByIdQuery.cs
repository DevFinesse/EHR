using EHR.Cqrs;
using EHR.IdentityService.Domain.Staff;

namespace EHR.IdentityService.Application.Staff;

public sealed record GetStaffUserByIdQuery(Guid Id) : IQuery<StaffUser?>;
