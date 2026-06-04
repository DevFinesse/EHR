using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.IdentityService.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("202606040001_AddTenantRegistrationReadModel")]
public sealed class AddTenantRegistrationReadModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("tenant_registrations", table => new
        {
            tenant_id = table.Column<string>(type: "text", nullable: false),
            hospital_id = table.Column<Guid>(type: "uuid", nullable: false),
            name = table.Column<string>(type: "text", nullable: false),
            registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            correlation_id = table.Column<string>(type: "text", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_tenant_registrations", x => x.tenant_id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("tenant_registrations");
    }
}
