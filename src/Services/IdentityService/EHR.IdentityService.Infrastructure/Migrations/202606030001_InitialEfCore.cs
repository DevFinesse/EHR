using EHR.IdentityService.Infrastructure.Staff;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.IdentityService.Infrastructure.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("202606030001_InitialEfCore")]
public sealed class InitialEfCore : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("staff_users", table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            tenant_id = table.Column<string>(type: "text", nullable: false),
            full_name = table.Column<string>(type: "text", nullable: false),
            email = table.Column<string>(type: "text", nullable: false),
            role = table.Column<string>(type: "text", nullable: false),
            department = table.Column<string>(type: "text", nullable: false),
            password_hash = table.Column<string>(type: "text", nullable: true),
            mfa_enabled = table.Column<bool>(type: "boolean", nullable: false),
            mfa_secret = table.Column<string>(type: "text", nullable: true),
            recovery_codes_hash = table.Column<string>(type: "text", nullable: true),
            failed_login_attempts = table.Column<int>(type: "integer", nullable: false),
            locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("pk_staff_users", x => x.id));

        migrationBuilder.CreateIndex("ix_staff_users_email", "staff_users", "email", unique: true);

        migrationBuilder.CreateTable("refresh_tokens", table => new
        {
            token = table.Column<string>(type: "text", nullable: false),
            staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_refresh_tokens", x => x.token));

        migrationBuilder.CreateTable("staff_invitations", table => new
        {
            token = table.Column<string>(type: "text", nullable: false),
            staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_staff_invitations", x => x.token));

        migrationBuilder.CreateTable("password_reset_tokens", table => new
        {
            token = table.Column<string>(type: "text", nullable: false),
            staff_user_id = table.Column<Guid>(type: "uuid", nullable: false),
            expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table => table.PrimaryKey("pk_password_reset_tokens", x => x.token));

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("password_reset_tokens");
        migrationBuilder.DropTable("staff_invitations");
        migrationBuilder.DropTable("refresh_tokens");
        migrationBuilder.DropTable("staff_users");
    }
}
