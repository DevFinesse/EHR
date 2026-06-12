using Dapper;
using EHR.Cqrs;
using EHR.PatientService.Application.Patients;
using EHR.PatientService.Domain.Patients;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class DapperPatientQueryHandler : IQueryHandler<GetPatientByIdQuery, Patient?>
{
    private readonly string _connectionString;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperPatientQueryHandler(string connectionString, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<Patient?> HandleAsync(GetPatientByIdQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   medical_record_number as MedicalRecordNumber,
                   full_name as FullName,
                   date_of_birth as DateOfBirth,
                   sex,
                   phone_number as PhoneNumber
            from patients
            where id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<PatientReadRow>(new CommandDefinition(sql, new { query.Id }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        _tenantAuthorization.EnsureCanAccessTenant(row.TenantId);
        return Patient.Restore(row.Id, row.TenantId, row.MedicalRecordNumber, row.FullName, row.DateOfBirth, row.Sex, row.PhoneNumber);
    }

    private sealed record PatientReadRow(Guid Id, string TenantId, string MedicalRecordNumber, string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber)
    {
        public PatientReadRow() : this(default, string.Empty, string.Empty, string.Empty, default, string.Empty, string.Empty) { }
    }
}
