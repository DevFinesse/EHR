using EHR.Cqrs;
using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Application.Tenants;
using EHR.IdentityService.Domain.Staff;
using EHR.Messaging;
using EHR.SharedKernel.Authorization;
using EHR.SharedKernel;

namespace EHR.IdentityService.Application.Auth;

public sealed class LoginHandler : ICommandHandler<LoginCommand, Result<TokenResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpProvider _totpProvider;
    private readonly IRecoveryCodeProtector _recoveryCodes;

    public LoginHandler(IStaffUserRepository staffUsers, IRefreshTokenRepository refreshTokens, ITokenIssuer tokenIssuer, IPasswordHasher passwordHasher, ITotpProvider totpProvider, IRecoveryCodeProtector recoveryCodes)
    {
        _staffUsers = staffUsers;
        _refreshTokens = refreshTokens;
        _tokenIssuer = tokenIssuer;
        _passwordHasher = passwordHasher;
        _totpProvider = totpProvider;
        _recoveryCodes = recoveryCodes;
    }

    public async Task<Result<TokenResponse>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await _staffUsers.GetByEmailAsync(command.Email, cancellationToken);
        if (staffUser is null)
        {
            return Result<TokenResponse>.Failure("Staff user was not found.");
        }

        if (staffUser.IsLocked(DateTimeOffset.UtcNow))
        {
            return Result<TokenResponse>.Failure("Staff user account is temporarily locked.");
        }

        if (staffUser.PasswordHash is null || !_passwordHasher.Verify(command.Password, staffUser.PasswordHash))
        {
            staffUser.RecordFailedLogin(DateTimeOffset.UtcNow);
            await _staffUsers.SaveAsync(staffUser, cancellationToken);
            return Result<TokenResponse>.Failure("Invalid credentials.");
        }

        if (staffUser.MfaEnabled && !MfaIsValid(staffUser, command.MfaCode))
        {
            return Result<TokenResponse>.Failure("A valid MFA code is required.");
        }

        staffUser.RecordSuccessfulLogin();
        await _staffUsers.SaveAsync(staffUser, cancellationToken);
        var token = await _tokenIssuer.IssueAsync(staffUser, cancellationToken);
        await _refreshTokens.StoreAsync(new RefreshTokenRecord(token.RefreshToken, staffUser.Id, DateTimeOffset.UtcNow.AddDays(14), null), cancellationToken);
        return Result<TokenResponse>.Success(token);
    }

    private bool MfaIsValid(StaffUser staffUser, string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(staffUser.MfaSecret))
        {
            return false;
        }

        if (_totpProvider.Verify(staffUser.MfaSecret, code, DateTimeOffset.UtcNow))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(staffUser.RecoveryCodesHash) && _recoveryCodes.TryConsume(code, staffUser.RecoveryCodesHash, out var updatedHash))
        {
            staffUser.ReplaceRecoveryCodesHash(updatedHash);
            return true;
        }

        return false;
    }
}

public sealed class InviteStaffHandler : ICommandHandler<InviteStaffCommand, Result<StaffInvitationResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly ITenantRegistrationReadModelRepository _tenants;
    private readonly ITenantAuthorizationService _tenantAuthorization;
    private readonly IStaffInvitationRepository _invitations;
    private readonly IEmailSender _emailSender;
    private readonly IStaffMetadataRepository _staffMetadata;
    private readonly ICurrentUserContext _currentUser;

    public InviteStaffHandler(IStaffUserRepository staffUsers, ITenantRegistrationReadModelRepository tenants, ITenantAuthorizationService tenantAuthorization, IStaffInvitationRepository invitations, IEmailSender emailSender, IStaffMetadataRepository staffMetadata, ICurrentUserContext currentUser)
    {
        _staffUsers = staffUsers;
        _tenants = tenants;
        _tenantAuthorization = tenantAuthorization;
        _invitations = invitations;
        _emailSender = emailSender;
        _staffMetadata = staffMetadata;
        _currentUser = currentUser;
    }

    public async Task<Result<StaffInvitationResponse>> HandleAsync(InviteStaffCommand command, CancellationToken cancellationToken)
    {
        var tenantId = command.TenantId.Trim();
        _tenantAuthorization.EnsureCanAccessTenant(tenantId);

        var tenant = await _tenants.GetByTenantIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return Result<StaffInvitationResponse>.Failure($"Tenant '{command.TenantId}' does not exist.");
        }

        var existing = await _staffUsers.GetByEmailAsync(command.Email, cancellationToken);
        if (existing is not null)
        {
            return Result<StaffInvitationResponse>.Failure("A staff user with this email already exists.");
        }

        var role = await _staffMetadata.NormalizeRoleAsync(tenantId, command.Role, cancellationToken);
        if (role is null)
        {
            return Result<StaffInvitationResponse>.Failure($"Unsupported staff role '{command.Role}'.");
        }

        var department = await _staffMetadata.NormalizeDepartmentAsync(tenantId, command.Department, cancellationToken);
        if (department is null)
        {
            return Result<StaffInvitationResponse>.Failure($"Unsupported staff department '{command.Department}'.");
        }

        var staffUser = StaffUser.Create(tenantId, command.FullName, command.Email, role, department);
        var integrationEvent = new StaffUserCreatedEvent(Guid.NewGuid(), staffUser.TenantId, staffUser.Id, staffUser.Role, _currentUser.CorrelationId);
        await _staffUsers.AddAsync(staffUser, integrationEvent, cancellationToken);

        var invitation = new StaffInvitation(CreateToken(), staffUser.Id, DateTimeOffset.UtcNow.AddDays(7), null);
        await _invitations.StoreAsync(invitation, cancellationToken);
        await _emailSender.SendAsync(staffUser.Email, "EHR staff invitation", $"Use invitation token {invitation.Token} to activate your staff account.", cancellationToken);
        return Result<StaffInvitationResponse>.Success(new StaffInvitationResponse(staffUser.Id, invitation.Token, invitation.ExpiresAt));
    }

    private static string CreateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}

public sealed class AcceptStaffInvitationHandler : ICommandHandler<AcceptStaffInvitationCommand, Result<TokenResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IStaffInvitationRepository _invitations;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpProvider _totpProvider;
    private readonly IRecoveryCodeProtector _recoveryCodes;

    public AcceptStaffInvitationHandler(IStaffUserRepository staffUsers, IStaffInvitationRepository invitations, IRefreshTokenRepository refreshTokens, ITokenIssuer tokenIssuer, IPasswordHasher passwordHasher, ITotpProvider totpProvider, IRecoveryCodeProtector recoveryCodes)
    {
        _staffUsers = staffUsers;
        _invitations = invitations;
        _refreshTokens = refreshTokens;
        _tokenIssuer = tokenIssuer;
        _passwordHasher = passwordHasher;
        _totpProvider = totpProvider;
        _recoveryCodes = recoveryCodes;
    }

    public async Task<Result<TokenResponse>> HandleAsync(AcceptStaffInvitationCommand command, CancellationToken cancellationToken)
    {
        var invitation = await _invitations.GetAsync(command.InvitationToken, cancellationToken);
        if (invitation is null || !invitation.IsActive(DateTimeOffset.UtcNow))
        {
            return Result<TokenResponse>.Failure("Invitation is invalid or expired.");
        }

        var staffUser = await _staffUsers.GetByIdAsync(invitation.StaffUserId, cancellationToken);
        if (staffUser is null)
        {
            return Result<TokenResponse>.Failure("Staff user was not found.");
        }

        if (staffUser.MfaEnabled && !MfaIsValid(staffUser, command.MfaCode))
        {
            return Result<TokenResponse>.Failure("A valid MFA code is required.");
        }

        var passwordResult = staffUser.SetPassword(_passwordHasher.Hash(command.Password));
        if (!passwordResult.IsSuccess)
        {
            return Result<TokenResponse>.Failure(passwordResult.Error!);
        }

        await _staffUsers.SaveAsync(staffUser, cancellationToken);
        await _invitations.MarkAcceptedAsync(invitation.Token, cancellationToken);
        var token = await _tokenIssuer.IssueAsync(staffUser, cancellationToken);
        await _refreshTokens.StoreAsync(new RefreshTokenRecord(token.RefreshToken, staffUser.Id, DateTimeOffset.UtcNow.AddDays(14), null), cancellationToken);
        return Result<TokenResponse>.Success(token);
    }

    private bool MfaIsValid(StaffUser staffUser, string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(staffUser.MfaSecret))
        {
            return false;
        }

        if (_totpProvider.Verify(staffUser.MfaSecret, code, DateTimeOffset.UtcNow))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(staffUser.RecoveryCodesHash) && _recoveryCodes.TryConsume(code, staffUser.RecoveryCodesHash, out var updatedHash))
        {
            staffUser.ReplaceRecoveryCodesHash(updatedHash);
            return true;
        }

        return false;
    }
}

public sealed class EnableMfaHandler : ICommandHandler<EnableMfaCommand, Result<bool>>
{
    private readonly IStaffUserRepository _staffUsers;

    public EnableMfaHandler(IStaffUserRepository staffUsers)
    {
        _staffUsers = staffUsers;
    }

    public async Task<Result<bool>> HandleAsync(EnableMfaCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await _staffUsers.GetByIdAsync(command.StaffUserId, cancellationToken);
        if (staffUser is null)
        {
            return Result<bool>.Failure("Staff user was not found.");
        }

        if (!staffUser.MfaEnabled)
        {
            return Result<bool>.Failure("MFA has not been set up.");
        }

        await _staffUsers.SaveAsync(staffUser, cancellationToken);
        return Result<bool>.Success(true);
    }
}

public sealed class SetupMfaHandler : ICommandHandler<SetupMfaCommand, Result<MfaSetupResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly ITotpProvider _totpProvider;
    private readonly IRecoveryCodeProtector _recoveryCodes;

    public SetupMfaHandler(IStaffUserRepository staffUsers, ITotpProvider totpProvider, IRecoveryCodeProtector recoveryCodes)
    {
        _staffUsers = staffUsers;
        _totpProvider = totpProvider;
        _recoveryCodes = recoveryCodes;
    }

    public async Task<Result<MfaSetupResponse>> HandleAsync(SetupMfaCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await _staffUsers.GetByIdAsync(command.StaffUserId, cancellationToken);
        if (staffUser is null)
        {
            return Result<MfaSetupResponse>.Failure("Staff user was not found.");
        }

        var secret = _totpProvider.CreateSecret();
        var codes = _recoveryCodes.GenerateCodes();
        var enabled = staffUser.EnableMfa(secret, _recoveryCodes.HashCodes(codes));
        if (!enabled.IsSuccess)
        {
            return Result<MfaSetupResponse>.Failure(enabled.Error!);
        }

        await _staffUsers.SaveAsync(staffUser, cancellationToken);
        return Result<MfaSetupResponse>.Success(new MfaSetupResponse(secret, codes));
    }
}

public sealed class ResetPasswordRequestHandler : ICommandHandler<ResetPasswordRequestCommand, Result<PasswordResetResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IEmailSender _emailSender;

    public ResetPasswordRequestHandler(IStaffUserRepository staffUsers, IPasswordResetTokenRepository resetTokens, IEmailSender emailSender)
    {
        _staffUsers = staffUsers;
        _resetTokens = resetTokens;
        _emailSender = emailSender;
    }

    public async Task<Result<PasswordResetResponse>> HandleAsync(ResetPasswordRequestCommand command, CancellationToken cancellationToken)
    {
        var staffUser = await _staffUsers.GetByEmailAsync(command.Email, cancellationToken);
        if (staffUser is null)
        {
            return Result<PasswordResetResponse>.Failure("Staff user was not found.");
        }

        var resetToken = new PasswordResetTokenRecord(CreateToken(), staffUser.Id, DateTimeOffset.UtcNow.AddHours(1), null);
        await _resetTokens.StoreAsync(resetToken, cancellationToken);
        await _emailSender.SendAsync(staffUser.Email, "EHR password reset", $"Use password reset token {resetToken.Token} to reset your password.", cancellationToken);
        return Result<PasswordResetResponse>.Success(new PasswordResetResponse(resetToken.Token, resetToken.ExpiresAt));
    }

    private static string CreateToken() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}

public sealed class ResetPasswordHandler : ICommandHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IPasswordResetTokenRepository _resetTokens;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(IStaffUserRepository staffUsers, IPasswordResetTokenRepository resetTokens, IPasswordHasher passwordHasher)
    {
        _staffUsers = staffUsers;
        _resetTokens = resetTokens;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var resetToken = await _resetTokens.GetAsync(command.ResetToken, cancellationToken);
        if (resetToken is null || !resetToken.IsActive(DateTimeOffset.UtcNow))
        {
            return Result<bool>.Failure("Password reset token is invalid or expired.");
        }

        var staffUser = await _staffUsers.GetByIdAsync(resetToken.StaffUserId, cancellationToken);
        if (staffUser is null)
        {
            return Result<bool>.Failure("Staff user was not found.");
        }

        var passwordResult = staffUser.SetPassword(_passwordHasher.Hash(command.NewPassword));
        if (!passwordResult.IsSuccess)
        {
            return Result<bool>.Failure(passwordResult.Error!);
        }

        await _staffUsers.SaveAsync(staffUser, cancellationToken);
        await _resetTokens.MarkUsedAsync(command.ResetToken, cancellationToken);
        return Result<bool>.Success(true);
    }
}

public sealed class RefreshAccessTokenHandler : ICommandHandler<RefreshAccessTokenCommand, Result<TokenResponse>>
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenIssuer _tokenIssuer;

    public RefreshAccessTokenHandler(IStaffUserRepository staffUsers, IRefreshTokenRepository refreshTokens, ITokenIssuer tokenIssuer)
    {
        _staffUsers = staffUsers;
        _refreshTokens = refreshTokens;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<TokenResponse>> HandleAsync(RefreshAccessTokenCommand command, CancellationToken cancellationToken)
    {
        var existing = await _refreshTokens.GetAsync(command.RefreshToken, cancellationToken);
        if (existing is null || !existing.IsActive(DateTimeOffset.UtcNow))
        {
            return Result<TokenResponse>.Failure("Refresh token is invalid or expired.");
        }

        var staffUser = await _staffUsers.GetByIdAsync(existing.StaffUserId, cancellationToken);
        if (staffUser is null)
        {
            return Result<TokenResponse>.Failure("Staff user was not found.");
        }

        await _refreshTokens.RevokeAsync(command.RefreshToken, cancellationToken);
        var token = await _tokenIssuer.IssueAsync(staffUser, cancellationToken);
        await _refreshTokens.StoreAsync(new RefreshTokenRecord(token.RefreshToken, staffUser.Id, DateTimeOffset.UtcNow.AddDays(14), null), cancellationToken);
        return Result<TokenResponse>.Success(token);
    }
}
