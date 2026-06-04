using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Domain.Staff;
using EHR.Messaging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EHR.IdentityService.Infrastructure.Staff;

public sealed class PostgresStaffUserRepository : IStaffUserRepository
{
    private const string EmailUniqueConstraint = "ix_staff_users_email";
    private readonly DbContextOptions<IdentityDbContext> _options;

    public PostgresStaffUserRepository(string connectionString) =>
        _options = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(connectionString).Options;

    public async Task AddAsync(StaffUser staffUser, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.StaffUsers.SingleOrDefaultAsync(existing => existing.Id == staffUser.Id, cancellationToken);
        if (row is null)
        {
            var existingEmailRow = await db.StaffUsers.AsNoTracking().SingleOrDefaultAsync(existing => existing.Email == staffUser.Email, cancellationToken);
            if (existingEmailRow is not null)
            {
                throw new DuplicateStaffUserEmailException(staffUser.Email);
            }

            db.StaffUsers.Add(ToRow(staffUser));
            db.OutboxMessages.Add(IdentityOutboxMessageRow.FromIntegrationEvent(integrationEvent));
        }
        else
        {
            Apply(row, staffUser);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateEmailException(exception))
        {
            throw new DuplicateStaffUserEmailException(staffUser.Email, exception);
        }
    }

    private static bool IsDuplicateEmailException(DbUpdateException exception)
        => exception.InnerException is PostgresException postgres
            && postgres.SqlState == "23505"
            && string.Equals(postgres.ConstraintName, EmailUniqueConstraint, StringComparison.Ordinal);

    public Task SaveAsync(StaffUser staffUser, CancellationToken cancellationToken) =>
        SaveOnlyAsync(staffUser, cancellationToken);

    private async Task SaveOnlyAsync(StaffUser staffUser, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        var row = await db.StaffUsers.SingleOrDefaultAsync(existing => existing.Id == staffUser.Id, cancellationToken);
        if (row is null)
        {
            db.StaffUsers.Add(ToRow(staffUser));
        }
        else
        {
            Apply(row, staffUser);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        GetAsync("id = @value", id, cancellationToken);

    public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        GetAsync("email = @value", email.Trim().ToLowerInvariant(), cancellationToken);

    private async Task<StaffUser?> GetAsync(string predicate, object value, CancellationToken cancellationToken)
    {
        await using var db = new IdentityDbContext(_options);
        StaffUserRow? row = predicate.StartsWith("id", StringComparison.Ordinal)
            ? await db.StaffUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Id == (Guid)value, cancellationToken)
            : await db.StaffUsers.AsNoTracking().SingleOrDefaultAsync(user => user.Email == (string)value, cancellationToken);

        return row is null ? null : ToDomain(row);
    }

    private static StaffUserRow ToRow(StaffUser staffUser) => new()
    {
        Id = staffUser.Id,
        TenantId = staffUser.TenantId,
        FullName = staffUser.FullName,
        Email = staffUser.Email,
        Role = staffUser.Role,
        Department = staffUser.Department,
        PasswordHash = staffUser.PasswordHash,
        MfaEnabled = staffUser.MfaEnabled,
        MfaSecret = staffUser.MfaSecret,
        RecoveryCodesHash = staffUser.RecoveryCodesHash,
        FailedLoginAttempts = staffUser.FailedLoginAttempts,
        LockedUntil = staffUser.LockedUntil,
        CreatedAt = staffUser.CreatedAt
    };

    private static void Apply(StaffUserRow row, StaffUser staffUser)
    {
        row.PasswordHash = staffUser.PasswordHash;
        row.MfaEnabled = staffUser.MfaEnabled;
        row.MfaSecret = staffUser.MfaSecret;
        row.RecoveryCodesHash = staffUser.RecoveryCodesHash;
        row.FailedLoginAttempts = staffUser.FailedLoginAttempts;
        row.LockedUntil = staffUser.LockedUntil;
    }

    private static StaffUser ToDomain(StaffUserRow row) =>
        StaffUser.Restore(row.Id, row.TenantId, row.FullName, row.Email, row.Role, row.Department, row.PasswordHash, row.MfaEnabled, row.MfaSecret, row.RecoveryCodesHash, row.FailedLoginAttempts, row.LockedUntil);
}
