using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.IdentityService.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("202606050001_AddTenantScopedStaffMetadata")]
public sealed class AddTenantScopedStaffMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("scope", "staff_roles", type: "text", nullable: false, defaultValue: string.Empty);
        migrationBuilder.AddColumn<string>("scope", "staff_departments", type: "text", nullable: false, defaultValue: string.Empty);
        migrationBuilder.AddColumn<string>("scope", "staff_role_permissions", type: "text", nullable: false, defaultValue: string.Empty);

        migrationBuilder.DropForeignKey("fk_staff_role_permissions_staff_roles_role_name", "staff_role_permissions");
        migrationBuilder.DropPrimaryKey("pk_staff_role_permissions", "staff_role_permissions");
        migrationBuilder.DropPrimaryKey("pk_staff_roles", "staff_roles");
        migrationBuilder.DropPrimaryKey("pk_staff_departments", "staff_departments");

        migrationBuilder.AddPrimaryKey("pk_staff_roles", "staff_roles", new[] { "scope", "name" });
        migrationBuilder.AddPrimaryKey("pk_staff_departments", "staff_departments", new[] { "scope", "name" });
        migrationBuilder.AddPrimaryKey("pk_staff_role_permissions", "staff_role_permissions", new[] { "scope", "role_name", "permission_name" });
        migrationBuilder.AddForeignKey(
            name: "fk_staff_role_permissions_staff_roles_scope_role_name",
            table: "staff_role_permissions",
            columns: new[] { "scope", "role_name" },
            principalTable: "staff_roles",
            principalColumns: new[] { "scope", "name" },
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("fk_staff_role_permissions_staff_roles_scope_role_name", "staff_role_permissions");
        migrationBuilder.DropPrimaryKey("pk_staff_role_permissions", "staff_role_permissions");
        migrationBuilder.DropPrimaryKey("pk_staff_roles", "staff_roles");
        migrationBuilder.DropPrimaryKey("pk_staff_departments", "staff_departments");

        migrationBuilder.AddPrimaryKey("pk_staff_roles", "staff_roles", "name");
        migrationBuilder.AddPrimaryKey("pk_staff_departments", "staff_departments", "name");
        migrationBuilder.AddPrimaryKey("pk_staff_role_permissions", "staff_role_permissions", new[] { "role_name", "permission_name" });
        migrationBuilder.AddForeignKey(
            "fk_staff_role_permissions_staff_roles_role_name",
            "staff_role_permissions",
            "role_name",
            "staff_roles",
            "name",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.DropColumn("scope", "staff_role_permissions");
        migrationBuilder.DropColumn("scope", "staff_roles");
        migrationBuilder.DropColumn("scope", "staff_departments");
    }
}
