using Dapper;
using EHR.AppointmentService.Application.Appointments;
using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class DapperListAppointmentsQueryHandler : IQueryHandler<ListAppointmentsQuery, IReadOnlyCollection<Appointment>>
{
    private readonly string _connectionString;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperListAppointmentsQueryHandler(string connectionString, ICurrentUserContext currentUser, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _currentUser = currentUser;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<IReadOnlyCollection<Appointment>> HandleAsync(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantFilter(query.TenantId);
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   patient_id as PatientId,
                   practitioner_id as PractitionerId,
                   scheduled_for as ScheduledFor,
                   reason,
                   status
            from appointments
            where (@TenantId is null or tenant_id = @TenantId)
              and (@From is null or scheduled_for >= @From)
              and (@To is null or scheduled_for <= @To)
              and (@PractitionerId is null or practitioner_id = @PractitionerId)
              and (@Status is null or status = @Status)
            order by scheduled_for desc
            limit @Limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<AppointmentReadRow>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            query.From,
            query.To,
            query.PractitionerId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            Limit = Clamp(query.Limit, 1, 200)
        }, cancellationToken: cancellationToken));

        return rows.Select(row => Appointment.Restore(row.Id, row.TenantId, row.PatientId, row.PractitionerId, row.ScheduledFor, row.Reason, row.Status)).ToArray();
    }

    private string? ResolveTenantFilter(string? requestedTenantId)
    {
        if (_currentUser.IsSuperAdmin)
        {
            return string.IsNullOrWhiteSpace(requestedTenantId) ? null : requestedTenantId.Trim();
        }

        if (string.IsNullOrWhiteSpace(_currentUser.TenantId))
        {
            throw new TenantAccessDeniedException(requestedTenantId ?? string.Empty);
        }

        if (!string.IsNullOrWhiteSpace(requestedTenantId))
        {
            _tenantAuthorization.EnsureCanAccessTenant(requestedTenantId);
        }

        return _currentUser.TenantId;
    }

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private sealed record AppointmentReadRow(Guid Id, string TenantId, Guid PatientId, Guid PractitionerId, DateTimeOffset ScheduledFor, string Reason, string Status);
}
