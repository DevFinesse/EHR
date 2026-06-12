using Dapper;
using EHR.Cqrs;
using EHR.TenantService.Application.Hospitals;
using EHR.TenantService.Domain.Hospitals;
using Npgsql;

namespace EHR.TenantService.Infrastructure.Hospitals;

public sealed class DapperHospitalQueryHandler : IQueryHandler<GetHospitalByIdQuery, Hospital?>
{
    private readonly string _connectionString;

    public DapperHospitalQueryHandler(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Hospital?> HandleAsync(GetHospitalByIdQuery query, CancellationToken cancellationToken)
    {
        const string sql = """
            select id, tenant_id as TenantId, name, country, city, plan
            from hospitals
            where id = @Id
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<HospitalReadRow>(new CommandDefinition(sql, new { query.Id }, cancellationToken: cancellationToken));
        return row is null ? null : Hospital.Restore(row.Id, row.TenantId, row.Name, row.Country, row.City, row.Plan);
    }

    private sealed record HospitalReadRow(Guid Id, string TenantId, string Name, string Country, string City, string Plan)
    {
        public HospitalReadRow() : this(default, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) { }
    }
}
