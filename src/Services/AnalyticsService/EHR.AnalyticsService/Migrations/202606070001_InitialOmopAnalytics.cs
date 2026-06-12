using EHR.AnalyticsService.Omop;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.AnalyticsService.Migrations;

[DbContext(typeof(OmopDbContext))]
[Migration("202606070001_InitialOmopAnalytics")]
public sealed class InitialOmopAnalytics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            create table if not exists omop_person (
                person_id uuid primary key,
                tenant_id text not null,
                source_patient_id uuid not null,
                person_source_value text not null,
                gender_source_value text null,
                birth_date date null,
                full_name text null,
                phone_number text null,
                updated_at timestamp with time zone not null
            );

            create unique index if not exists ux_omop_person_source on omop_person (tenant_id, source_patient_id);
            create index if not exists ix_omop_person_tenant on omop_person (tenant_id);

            create table if not exists omop_visit_occurrence (
                visit_occurrence_id uuid primary key,
                tenant_id text not null,
                person_id uuid not null,
                source_encounter_id uuid not null,
                source_appointment_id uuid null,
                provider_id uuid null,
                visit_source_value text null,
                visit_start_datetime timestamp with time zone not null,
                visit_end_datetime timestamp with time zone null
            );

            create unique index if not exists ux_omop_visit_source on omop_visit_occurrence (tenant_id, source_encounter_id);
            create index if not exists ix_omop_visit_person on omop_visit_occurrence (person_id);

            create table if not exists omop_condition_occurrence (
                condition_occurrence_id uuid primary key,
                tenant_id text not null,
                person_id uuid not null,
                visit_occurrence_id uuid not null,
                condition_source_value text not null,
                condition_concept_id bigint not null default 0,
                condition_source_concept_id bigint not null default 0,
                condition_source_text text null,
                condition_status_source_value text null,
                condition_start_datetime timestamp with time zone not null,
                source_event_id uuid not null
            );

            create unique index if not exists ux_omop_condition_source on omop_condition_occurrence (source_event_id);
            create index if not exists ix_omop_condition_visit on omop_condition_occurrence (visit_occurrence_id);

            create table if not exists omop_measurement (
                measurement_id uuid primary key,
                tenant_id text not null,
                person_id uuid not null,
                visit_occurrence_id uuid not null,
                measurement_source_value text not null,
                measurement_concept_id bigint not null default 0,
                measurement_source_concept_id bigint not null default 0,
                value_as_number numeric not null,
                unit_source_value text not null,
                unit_concept_id bigint not null default 0,
                unit_source_concept_id bigint not null default 0,
                measurement_datetime timestamp with time zone not null,
                source_event_id uuid not null
            );

            create unique index if not exists ux_omop_measurement_source on omop_measurement (source_event_id, measurement_source_value);
            create index if not exists ix_omop_measurement_visit on omop_measurement (visit_occurrence_id);

            create table if not exists omop_concept_map (
                id uuid primary key,
                domain text not null,
                source_vocabulary text not null,
                source_code text not null,
                source_name text null,
                source_concept_id bigint not null default 0,
                standard_vocabulary text not null,
                standard_concept_id bigint not null,
                standard_concept_code text not null,
                standard_concept_name text not null,
                created_at timestamp with time zone not null,
                updated_at timestamp with time zone not null
            );

            create unique index if not exists ux_omop_concept_map_source on omop_concept_map (domain, source_vocabulary, source_code);
            """);

        migrationBuilder.Sql(InboxSql.CreateTable);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("inbox_messages");
        migrationBuilder.DropTable("omop_measurement");
        migrationBuilder.DropTable("omop_condition_occurrence");
        migrationBuilder.DropTable("omop_visit_occurrence");
        migrationBuilder.DropTable("omop_person");
        migrationBuilder.DropTable("omop_concept_map");
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
