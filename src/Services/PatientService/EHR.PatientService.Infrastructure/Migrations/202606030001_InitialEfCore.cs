using EHR.PatientService.Infrastructure.Patients;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.PatientService.Infrastructure.Migrations;

[DbContext(typeof(PatientDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("patients", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            medical_record_number = table.Column<string>(type: "text", nullable: false),
            full_name = table.Column<string>(type: "text", nullable: false),
            date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
            sex = table.Column<string>(type: "text", nullable: false),
            phone_number = table.Column<string>(type: "text", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_patients", x => x.id));

        migrationBuilder.CreateIndex("ix_patients_tenant_id", "patients", "tenant_id");
        migrationBuilder.CreateIndex("ix_patients_medical_record_number", "patients", "medical_record_number", unique: true);

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
        migrationBuilder.DropTable("patients");
    }
}
