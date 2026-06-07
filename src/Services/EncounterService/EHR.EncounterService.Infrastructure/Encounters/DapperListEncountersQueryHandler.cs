using System.Text.Json;
using Dapper;
using EHR.Cqrs;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class DapperListEncountersQueryHandler : IQueryHandler<ListEncountersQuery, IReadOnlyCollection<Encounter>>
{
    private readonly string _connectionString;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperListEncountersQueryHandler(string connectionString, ICurrentUserContext currentUser, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _currentUser = currentUser;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<IReadOnlyCollection<Encounter>> HandleAsync(ListEncountersQuery query, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantFilter(query.TenantId);
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
            where (@TenantId is null or tenant_id = @TenantId)
              and (@PatientId is null or patient_id = @PatientId)
              and (@Status is null or status = @Status)
            order by id desc
            limit @Limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<EncounterReadRow>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            query.PatientId,
            Status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim(),
            Limit = Clamp(query.Limit, 1, 200)
        }, cancellationToken: cancellationToken));

        return rows.Select(ToDomain).ToArray();
    }

    private Encounter ToDomain(EncounterReadRow row)
    {
        var vitals = JsonSerializer.Deserialize<IReadOnlyCollection<VitalSigns>>(row.VitalsJson) ?? [];
        var diagnoses = JsonSerializer.Deserialize<IReadOnlyCollection<Diagnosis>>(row.DiagnosesJson) ?? [];
        return Encounter.Restore(row.Id, row.TenantId, row.AppointmentId, row.PatientId, row.PractitionerId, row.VisitType, row.Status, vitals, diagnoses);
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

    private sealed record EncounterReadRow(Guid Id, string TenantId, Guid AppointmentId, Guid PatientId, Guid PractitionerId, string VisitType, string Status, string VitalsJson, string DiagnosesJson);
}
