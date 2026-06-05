using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.IdentityService.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("202606040003_AddStaffMetadata")]
public sealed class AddStaffMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("staff_departments", table => new
        {
            name = table.Column<string>(type: "text", nullable: false),
            description = table.Column<string>(type: "text", nullable: true),
            is_system = table.Column<bool>(type: "boolean", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_staff_departments", x => x.name));

        migrationBuilder.CreateTable("staff_permissions", table => new
        {
            name = table.Column<string>(type: "text", nullable: false),
            description = table.Column<string>(type: "text", nullable: true),
            is_system = table.Column<bool>(type: "boolean", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_staff_permissions", x => x.name));

        migrationBuilder.CreateTable("staff_roles", table => new
        {
            name = table.Column<string>(type: "text", nullable: false),
            description = table.Column<string>(type: "text", nullable: true),
            is_system = table.Column<bool>(type: "boolean", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_staff_roles", x => x.name));

        migrationBuilder.CreateTable("staff_role_permissions", table => new
        {
            role_name = table.Column<string>(type: "text", nullable: false),
            permission_name = table.Column<string>(type: "text", nullable: false),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("pk_staff_role_permissions", x => new { x.role_name, x.permission_name });
            table.ForeignKey("fk_staff_role_permissions_staff_permissions_permission_name", x => x.permission_name, "staff_permissions", "name", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("fk_staff_role_permissions_staff_roles_role_name", x => x.role_name, "staff_roles", "name", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("ix_staff_role_permissions_permission_name", "staff_role_permissions", "permission_name");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("staff_role_permissions");
        migrationBuilder.DropTable("staff_departments");
        migrationBuilder.DropTable("staff_permissions");
        migrationBuilder.DropTable("staff_roles");
    }
}
