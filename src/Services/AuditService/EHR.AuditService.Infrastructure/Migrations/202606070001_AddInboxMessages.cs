using EHR.AuditService.Infrastructure.AuditRecords;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.AuditService.Infrastructure.Migrations;

[DbContext(typeof(AuditDbContext))]
[Migration("202606070001_AddInboxMessages")]
public sealed class AddInboxMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(InboxSql.CreateTable);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("inbox_messages");
    }
}

internal static class InboxSql
{
    public const string CreateTable = """
        create table if not exists inbox_messages (
            event_id uuid not null,
            consumer_group text not null,
            event_type text not null,
            tenant_id text not null,
            correlation_id text not null,
            topic text not null,
            partition integer not null,
            offset_value bigint not null,
            attempts integer not null default 0,
            status text not null,
            last_error text null,
            received_at timestamp with time zone not null,
            processed_at timestamp with time zone null,
            dead_lettered_at timestamp with time zone null,
            constraint pk_inbox_messages primary key (event_id, consumer_group)
        );

        create index if not exists ix_inbox_messages_status on inbox_messages (status);
        create index if not exists ix_inbox_messages_event_type on inbox_messages (event_type);
        create index if not exists ix_inbox_messages_received_at on inbox_messages (received_at);
        """;
}
