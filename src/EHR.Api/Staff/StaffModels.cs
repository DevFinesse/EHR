using EHR.SharedKernel;

namespace EHR.Api.Staff;

public sealed class StaffUser : Entity
{
    public StaffUser(Guid id, string tenantId, string fullName, string email, string role, string department)
        : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
        Role = role;
        Department = department;
    }

    public string TenantId { get; }
    public string FullName { get; }
    public string Email { get; }
    public string Role { get; }
    public string Department { get; }
}

public sealed record CreateStaffUserCommand(string FullName, string Email, string Role, string Department);
