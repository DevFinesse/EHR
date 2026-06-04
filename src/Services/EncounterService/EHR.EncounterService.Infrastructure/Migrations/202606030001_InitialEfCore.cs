using EHR.EncounterService.Infrastructure.Encounters;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.EncounterService.Infrastructure.Migrations;

[DbContext(typeof(EncounterDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("encounters", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
            patient_id = table.Column<Guid>(type: "uuid", nullable: false),
            practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
            visit_type = table.Column<string>(type: "text", nullable: false),
            status = table.Column<string>(type: "text", nullable: false),
            vitals = table.Column<string>(type: "jsonb", nullable: false),
            diagnoses = table.Column<string>(type: "jsonb", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_encounters", x => x.id));

        migrationBuilder.CreateIndex("ix_encounters_tenant_id", "encounters", "tenant_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("encounters");
    }
}
