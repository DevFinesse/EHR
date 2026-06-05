using EHR.IdentityService.Domain.Staff;

namespace EHR.IdentityService.Application.Auth;

public interface ITokenIssuer
{
    Task<TokenResponse> IssueAsync(StaffUser staffUser, CancellationToken cancellationToken);
}

public interface IRefreshTokenRepository
{
    Task StoreAsync(RefreshTokenRecord refreshToken, CancellationToken cancellationToken);

    Task<RefreshTokenRecord?> GetAsync(string token, CancellationToken cancellationToken);

    Task RevokeAsync(string token, CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public interface ITotpProvider
{
    string CreateSecret();

    bool Verify(string secret, string code, DateTimeOffset now);
}

public interface IRecoveryCodeProtector
{
    IReadOnlyCollection<string> GenerateCodes(int count = 10);

    string HashCodes(IEnumerable<string> codes);

    bool TryConsume(string code, string recoveryCodesHash, out string updatedRecoveryCodesHash);
}

public interface IStaffInvitationRepository
{
    Task StoreAsync(StaffInvitation invitation, CancellationToken cancellationToken);

    Task<StaffInvitation?> GetAsync(string token, CancellationToken cancellationToken);

    Task MarkAcceptedAsync(string token, CancellationToken cancellationToken);
}

public interface IPasswordResetTokenRepository
{
    Task StoreAsync(PasswordResetTokenRecord resetToken, CancellationToken cancellationToken);

    Task<PasswordResetTokenRecord?> GetAsync(string token, CancellationToken cancellationToken);

    Task MarkUsedAsync(string token, CancellationToken cancellationToken);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
