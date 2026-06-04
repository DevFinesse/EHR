namespace EHR.IdentityService.Application.Staff;

public sealed class DuplicateStaffUserEmailException : InvalidOperationException
{
    public string Email { get; }

    public DuplicateStaffUserEmailException(string email)
        : base("A staff user with this email already exists.")
    {
        Email = email;
    }

    public DuplicateStaffUserEmailException(string email, Exception innerException)
        : base("A staff user with this email already exists.", innerException)
    {
        Email = email;
    }
}
