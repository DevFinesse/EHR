using Dapper;
using EHR.Cqrs;
using EHR.PatientService.Application.Patients;
using EHR.PatientService.Domain.Patients;
using EHR.SharedKernel.Authorization;
using Npgsql;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class DapperSearchPatientsQueryHandler : IQueryHandler<SearchPatientsQuery, IReadOnlyCollection<Patient>>
{
    private readonly string _connectionString;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITenantAuthorizationService _tenantAuthorization;

    public DapperSearchPatientsQueryHandler(string connectionString, ICurrentUserContext currentUser, ITenantAuthorizationService tenantAuthorization)
    {
        _connectionString = connectionString;
        _currentUser = currentUser;
        _tenantAuthorization = tenantAuthorization;
    }

    public async Task<IReadOnlyCollection<Patient>> HandleAsync(SearchPatientsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = ResolveTenantFilter(query.TenantId);
        const string sql = """
            select id,
                   tenant_id as TenantId,
                   medical_record_number as MedicalRecordNumber,
                   full_name as FullName,
                   date_of_birth as DateOfBirth,
                   sex,
                   phone_number as PhoneNumber
            from patients
            where (@TenantId is null or tenant_id = @TenantId)
              and (@MedicalRecordNumber is null or medical_record_number ilike @MedicalRecordNumber)
              and (@Name is null or full_name ilike @Name)
              and (@PhoneNumber is null or phone_number ilike @PhoneNumber)
            order by full_name
            limit @Limit
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var rows = await connection.QueryAsync<PatientReadRow>(new CommandDefinition(sql, new
        {
            TenantId = tenantId,
            MedicalRecordNumber = Like(query.MedicalRecordNumber),
            Name = Like(query.Name),
            PhoneNumber = Like(query.PhoneNumber),
            Limit = Clamp(query.Limit, 1, 200)
        }, cancellationToken: cancellationToken));

        return rows.Select(row => Patient.Restore(row.Id, row.TenantId, row.MedicalRecordNumber, row.FullName, row.DateOfBirth, row.Sex, row.PhoneNumber)).ToArray();
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

    private static string? Like(string? value) => string.IsNullOrWhiteSpace(value) ? null : $"%{value.Trim()}%";

    private static int Clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

    private sealed record PatientReadRow(Guid Id, string TenantId, string MedicalRecordNumber, string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber)
    {
        public PatientReadRow() : this(default, string.Empty, string.Empty, string.Empty, default, string.Empty, string.Empty) { }
    }
}
