using EHR.Cqrs;
using EHR.SharedKernel;

namespace EHR.IdentityService.Application.Auth;

public sealed record LoginCommand(string Email, string Password, string? MfaCode) : ICommand<Result<TokenResponse>>;

public sealed record RefreshAccessTokenCommand(string RefreshToken) : ICommand<Result<TokenResponse>>;

public sealed record InviteStaffCommand(string TenantId, string FullName, string Email, string Role, string Department) : ICommand<Result<StaffInvitationResponse>>;

public sealed record AcceptStaffInvitationCommand(string InvitationToken, string Password, string? MfaCode) : ICommand<Result<TokenResponse>>;

public sealed record EnableMfaCommand(Guid StaffUserId) : ICommand<Result<bool>>;

public sealed record SetupMfaCommand(Guid StaffUserId) : ICommand<Result<MfaSetupResponse>>;

public sealed record ResetPasswordRequestCommand(string Email) : ICommand<Result<PasswordResetResponse>>;

public sealed record ResetPasswordCommand(string ResetToken, string NewPassword) : ICommand<Result<bool>>;

public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenRecord(string Token, Guid StaffUserId, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt)
{
    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;
}

public sealed record StaffInvitation(string Token, Guid StaffUserId, DateTimeOffset ExpiresAt, DateTimeOffset? AcceptedAt)
{
    public bool IsActive(DateTimeOffset now) => AcceptedAt is null && ExpiresAt > now;
}

public sealed record StaffInvitationResponse(Guid StaffUserId, string InvitationToken, DateTimeOffset ExpiresAt);

public sealed record MfaSetupResponse(string Secret, IReadOnlyCollection<string> RecoveryCodes);

public sealed record PasswordResetTokenRecord(string Token, Guid StaffUserId, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt)
{
    public bool IsActive(DateTimeOffset now) => UsedAt is null && ExpiresAt > now;
}

public sealed record PasswordResetResponse(string ResetToken, DateTimeOffset ExpiresAt);
