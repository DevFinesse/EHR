using EHR.PatientService.Infrastructure.Patients;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.PatientService.Infrastructure.Migrations;

[DbContext(typeof(PatientDbContext))]
[Migration("202606040001_AddOutboxMessages")]
public sealed class AddOutboxMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("outbox_messages", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            event_id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            type = table.Column<string>(type: "text", nullable: false),
            occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            correlation_id = table.Column<string>(type: "text", nullable: false),
            payload = table.Column<string>(type: "jsonb", nullable: false),
            attempts = table.Column<int>(type: "integer", nullable: false),
            last_error = table.Column<string>(type: "text", nullable: true),
            processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_outbox_messages", x => x.id));

        migrationBuilder.CreateIndex(
            "ix_outbox_messages_processed_at_occurred_at",
            "outbox_messages",
            new[] { "processed_at", "occurred_at" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("outbox_messages");
    }
}
