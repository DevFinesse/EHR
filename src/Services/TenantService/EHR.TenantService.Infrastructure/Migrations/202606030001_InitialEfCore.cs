using EHR.TenantService.Infrastructure.Hospitals;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.TenantService.Infrastructure.Migrations;

[DbContext(typeof(TenantDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "hospitals",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                tenant_id = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                country = table.Column<string>(type: "text", nullable: false),
                city = table.Column<string>(type: "text", nullable: false),
                plan = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_hospitals", x => x.id));

        migrationBuilder.CreateIndex("ix_hospitals_tenant_id", "hospitals", "tenant_id", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("hospitals");
    }
}
