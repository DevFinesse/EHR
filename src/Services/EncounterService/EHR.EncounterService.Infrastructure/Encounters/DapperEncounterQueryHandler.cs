using System.Text.Json;
using Dapper;
using EHR.Cqrs;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class DapperEncounterQueryHandler : IQueryHandler<GetEncounterByIdQuery, Encounter?>
{
    private readonly string _connectionString;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperEncounterQueryHandler(string connectionString, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Encounter?> HandleAsync(GetEncounterByIdQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   appointment_id as AppointmentId,
                   patient_id as PatientId,
                   practitioner_id as PractitionerId,
                   visit_type as VisitType,
                   status,
                   vitals as VitalsJson,
                   diagnoses as DiagnosesJson
            from encounters
            where id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<EncounterReadRow>(new CommandDefinition(sql, new { query.Id }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        _tenantAuthorization.EnsureCanAccessTenant(row.TenantId);
        var vitals = JsonSerializer.Deserialize<IReadOnlyCollection<VitalSigns>>(row.VitalsJson) ?? [];
        var diagnoses = JsonSerializer.Deserialize<IReadOnlyCollection<Diagnosis>>(row.DiagnosesJson) ?? [];
        return Encounter.Restore(row.Id, row.TenantId, row.AppointmentId, row.PatientId, row.PractitionerId, row.VisitType, row.Status, vitals, diagnoses);
    }

    private sealed record EncounterReadRow(Guid Id, string TenantId, Guid AppointmentId, Guid PatientId, Guid PractitionerId, string VisitType, string Status, string VitalsJson, string DiagnosesJson);
}
