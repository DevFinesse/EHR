using Dapper;
using EHR.AppointmentService.Application.Appointments;
using EHR.AppointmentService.Domain.Appointments;
using EHR.Cqrs;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.AppointmentService.Infrastructure.Appointments;

public sealed class DapperAppointmentQueryHandler : IQueryHandler<GetAppointmentByIdQuery, Appointment?>
{
    private readonly string _connectionString;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperAppointmentQueryHandler(string connectionString, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Appointment?> HandleAsync(GetAppointmentByIdQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   patient_id as PatientId,
                   practitioner_id as PractitionerId,
                   scheduled_for as ScheduledFor,
                   reason,
                   status
            from appointments
            where id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<AppointmentReadRow>(new CommandDefinition(sql, new { query.Id }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        _tenantAuthorization.EnsureCanAccessTenant(row.TenantId);
        return Appointment.Restore(row.Id, row.TenantId, row.PatientId, row.PractitionerId, row.ScheduledFor, row.Reason, row.Status);
    }

    private sealed record AppointmentReadRow(Guid Id, string TenantId, Guid PatientId, Guid PractitionerId, DateTimeOffset ScheduledFor, string Reason, string Status)
    {
        public AppointmentReadRow() : this(default, string.Empty, default, default, default, string.Empty, string.Empty) { }
    }
}
