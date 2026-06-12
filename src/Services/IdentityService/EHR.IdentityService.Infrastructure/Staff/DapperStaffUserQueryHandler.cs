using Dapper;
using EHR.Cqrs;
using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Domain.Staff;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class DapperStaffUserQueryHandler : IQueryHandler<GetStaffUserByIdQuery, StaffUser?>
{
    private readonly string _connectionString;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperStaffUserQueryHandler(string connectionString, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<StaffUser?> HandleAsync(GetStaffUserByIdQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   full_name as FullName,
                   email,
                   role,
                   department,
                   password_hash as PasswordHash,
                   mfa_enabled as MfaEnabled,
                   mfa_secret as MfaSecret,
                   recovery_codes_hash as RecoveryCodesHash,
                   failed_login_attempts as FailedLoginAttempts,
                   locked_until as LockedUntil
            from staff_users
            where id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<StaffUserReadRow>(new CommandDefinition(sql, new { query.Id }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        _tenantAuthorization.EnsureCanAccessTenant(row.TenantId);
        return StaffUser.Restore(row.Id, row.TenantId, row.FullName, row.Email, row.Role, row.Department, row.PasswordHash, row.MfaEnabled, row.MfaSecret, row.RecoveryCodesHash, row.FailedLoginAttempts, row.LockedUntil);
    }

    private sealed record StaffUserReadRow(Guid Id, string TenantId, string FullName, string Email, string Role, string Department, string? PasswordHash, bool MfaEnabled, string? MfaSecret, string? RecoveryCodesHash, int FailedLoginAttempts, DateTimeOffset? LockedUntil)
    {
        public StaffUserReadRow() : this(default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, false, null, null, 0, null) { }
    }
}
