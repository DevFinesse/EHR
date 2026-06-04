using EHR.SharedKernel;
using EHR.SharedKernel.Authorization;

namespace EHR.IdentityService.Domain.Staff;

public sealed class StaffUser
{
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private StaffUser(
        Guid id,
        string tenantId,
        string fullName,
        string email,
        string role,
        string department,
        string? passwordHash,
        bool mfaEnabled,
        string? mfaSecret,
        string? recoveryCodesHash,
        int failedLoginAttempts,
        DateTimeOffset? lockedUntil)
    {
        Id = id;
        TenantId = tenantId;
        FullName = fullName;
        Email = email;
        Role = role;
        Department = department;
        PasswordHash = passwordHash;
        MfaEnabled = mfaEnabled;
        MfaSecret = mfaSecret;
        RecoveryCodesHash = recoveryCodesHash;
        FailedLoginAttempts = failedLoginAttempts;
        LockedUntil = lockedUntil;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public string FullName { get; }
    public string Email { get; }
    public string Role { get; }
    public string Department { get; }
    public string? PasswordHash { get; private set; }
    public bool MfaEnabled { get; private set; }
    public string? MfaSecret { get; private set; }
    public string? RecoveryCodesHash { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    public static StaffUser Create(string tenantId, string fullName, string email, string role, string department)
    {
        if (!PlatformRoles.TryNormalize(role, out var normalizedRole))
        {
            throw new ArgumentException($"Unsupported staff role '{role}'.", nameof(role));
        }

        if (!StaffDepartments.TryNormalize(department, out var normalizedDepartment))
        {
            throw new ArgumentException($"Unsupported staff department '{department}'.", nameof(department));
        }

        return new(Guid.NewGuid(), tenantId.Trim(), fullName.Trim(), email.Trim().ToLowerInvariant(), normalizedRole, normalizedDepartment, null, false, null, null, 0, null);
    }

    public static StaffUser Restore(Guid id, string tenantId, string fullName, string email, string role, string department, string? passwordHash, bool mfaEnabled, string? mfaSecret, string? recoveryCodesHash, int failedLoginAttempts, DateTimeOffset? lockedUntil) =>
        new(id, tenantId, fullName, email, role, department, passwordHash, mfaEnabled, mfaSecret, recoveryCodesHash, failedLoginAttempts, lockedUntil);

    public Result<StaffUser> SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<StaffUser>.Failure("Password hash is required.");
        }

        PasswordHash = passwordHash;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        return Result<StaffUser>.Success(this);
    }

    public Result<StaffUser> EnableMfa(string secret, string recoveryCodesHash)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(recoveryCodesHash))
        {
            return Result<StaffUser>.Failure("MFA secret and recovery codes are required.");
        }

        MfaEnabled = true;
        MfaSecret = secret;
        RecoveryCodesHash = recoveryCodesHash;
        return Result<StaffUser>.Success(this);
    }

    public void ReplaceRecoveryCodesHash(string recoveryCodesHash) => RecoveryCodesHash = recoveryCodesHash;

    public bool IsLocked(DateTimeOffset now) => LockedUntil is not null && LockedUntil > now;

    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public void RecordFailedLogin(DateTimeOffset now)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockedUntil = now.Add(LockoutDuration);
        }
    }
}
