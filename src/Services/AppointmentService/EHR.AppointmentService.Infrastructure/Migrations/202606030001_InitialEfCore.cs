using EHR.AppointmentService.Infrastructure.Appointments;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.AppointmentService.Infrastructure.Migrations;

[DbContext(typeof(AppointmentDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("appointments", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            patient_id = table.Column<Guid>(type: "uuid", nullable: false),
            practitioner_id = table.Column<Guid>(type: "uuid", nullable: false),
            scheduled_for = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            reason = table.Column<string>(type: "text", nullable: false),
            status = table.Column<string>(type: "text", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_appointments", x => x.id));

        migrationBuilder.CreateIndex("ix_appointments_tenant_id", "appointments", "tenant_id");

        migrationBuilder.CreateTable("known_patients", table => new
        {
            patient_id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            medical_record_number = table.Column<string>(type: "text", nullable: false),
            registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            correlation_id = table.Column<string>(type: "text", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_known_patients", x => x.patient_id));

        migrationBuilder.CreateIndex("ix_known_patients_tenant_id", "known_patients", "tenant_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("known_patients");
        migrationBuilder.DropTable("appointments");
    }
}
