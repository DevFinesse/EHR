using EHR.Cqrs;
using EHR.IdentityService.Domain.Staff;

namespace EHR.IdentityService.Application.Staff;

public sealed record CreateStaffUserCommand(string TenantId, string FullName, string Email, string Role, string Department) : ICommand<StaffUser>;
