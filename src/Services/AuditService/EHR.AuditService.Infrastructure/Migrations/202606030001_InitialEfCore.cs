using EHR.AuditService.Infrastructure.AuditRecords;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.AuditService.Infrastructure.Migrations;

[DbContext(typeof(AuditDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("audit_records", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            action = table.Column<string>(type: "text", nullable: false),
            resource_type = table.Column<string>(type: "text", nullable: false),
            resource_id = table.Column<string>(type: "text", nullable: false),
            severity = table.Column<string>(type: "text", nullable: false),
            timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            correlation_id = table.Column<string>(type: "text", nullable: false),
            user_id = table.Column<string>(type: "text", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_audit_records", x => x.id));

        migrationBuilder.CreateIndex("ix_audit_records_tenant_id", "audit_records", "tenant_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("audit_records");
    }
}
