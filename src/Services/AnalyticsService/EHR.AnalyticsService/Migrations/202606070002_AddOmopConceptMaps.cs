using EHR.AnalyticsService.Omop;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EHR.AnalyticsService.Migrations;

[DbContext(typeof(OmopDbContext))]
[Migration("202606070002_AddOmopConceptMaps")]
public sealed class AddOmopConceptMaps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            alter table omop_condition_occurrence
                add column if not exists condition_concept_id bigint not null default 0,
                add column if not exists condition_source_concept_id bigint not null default 0;

            alter table omop_measurement
                add column if not exists measurement_concept_id bigint not null default 0,
                add column if not exists measurement_source_concept_id bigint not null default 0,
                add column if not exists unit_concept_id bigint not null default 0,
                add column if not exists unit_source_concept_id bigint not null default 0;

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

            create unique index if not exists ux_omop_concept_map_source
                on omop_concept_map (domain, source_vocabulary, source_code);

            insert into omop_concept_map (
                id, domain, source_vocabulary, source_code, source_name, source_concept_id,
                standard_vocabulary, standard_concept_id, standard_concept_code, standard_concept_name,
                created_at, updated_at)
            values
                ('00000000-0000-0000-0000-000000001001', 'Measurement', 'EHR', 'temperature_celsius', 'Body temperature', 0, 'LOINC', 3020891, '8310-5', 'Body temperature', now(), now()),
                ('00000000-0000-0000-0000-000000001002', 'Measurement', 'EHR', 'systolic_blood_pressure', 'Systolic blood pressure', 0, 'LOINC', 3004249, '8480-6', 'Systolic blood pressure', now(), now()),
                ('00000000-0000-0000-0000-000000001003', 'Measurement', 'EHR', 'diastolic_blood_pressure', 'Diastolic blood pressure', 0, 'LOINC', 3012888, '8462-4', 'Diastolic blood pressure', now(), now()),
                ('00000000-0000-0000-0000-000000001004', 'Measurement', 'EHR', 'pulse_rate', 'Pulse rate', 0, 'LOINC', 3027018, '8867-4', 'Heart rate', now(), now()),
                ('00000000-0000-0000-0000-000000001005', 'Measurement', 'EHR', 'oxygen_saturation', 'Oxygen saturation', 0, 'LOINC', 3016502, '59408-5', 'Oxygen saturation in Arterial blood by Pulse oximetry', now(), now()),
                ('00000000-0000-0000-0000-000000002001', 'Unit', 'UCUM', 'Cel', 'degree Celsius', 0, 'UCUM', 586323, 'Cel', 'degree Celsius', now(), now()),
                ('00000000-0000-0000-0000-000000002002', 'Unit', 'UCUM', 'mm[Hg]', 'millimeter of mercury', 0, 'UCUM', 8876, 'mm[Hg]', 'millimeter of mercury', now(), now()),
                ('00000000-0000-0000-0000-000000002003', 'Unit', 'UCUM', '/min', 'per minute', 0, 'UCUM', 8541, '/min', 'per minute', now(), now()),
                ('00000000-0000-0000-0000-000000002004', 'Unit', 'UCUM', '%', 'percent', 0, 'UCUM', 8554, '%', 'percent', now(), now()),
                ('00000000-0000-0000-0000-000000003001', 'Condition', 'ICD10', 'I10', 'Essential hypertension', 0, 'SNOMED', 320128, '59621000', 'Essential hypertension', now(), now()),
                ('00000000-0000-0000-0000-000000003002', 'Condition', 'ICD10', 'E11', 'Type 2 diabetes mellitus', 0, 'SNOMED', 201826, '44054006', 'Type 2 diabetes mellitus', now(), now()),
                ('00000000-0000-0000-0000-000000003003', 'Condition', 'SNOMED', '59621000', 'Essential hypertension', 320128, 'SNOMED', 320128, '59621000', 'Essential hypertension', now(), now()),
                ('00000000-0000-0000-0000-000000003004', 'Condition', 'SNOMED', '44054006', 'Type 2 diabetes mellitus', 201826, 'SNOMED', 201826, '44054006', 'Type 2 diabetes mellitus', now(), now())
            on conflict (domain, source_vocabulary, source_code) do update
            set source_name = excluded.source_name,
                source_concept_id = excluded.source_concept_id,
                standard_vocabulary = excluded.standard_vocabulary,
                standard_concept_id = excluded.standard_concept_id,
                standard_concept_code = excluded.standard_concept_code,
                standard_concept_name = excluded.standard_concept_name,
                updated_at = now();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("omop_concept_map");
        migrationBuilder.DropColumn("condition_concept_id", "omop_condition_occurrence");
        migrationBuilder.DropColumn("condition_source_concept_id", "omop_condition_occurrence");
        migrationBuilder.DropColumn("measurement_concept_id", "omop_measurement");
        migrationBuilder.DropColumn("measurement_source_concept_id", "omop_measurement");
        migrationBuilder.DropColumn("unit_concept_id", "omop_measurement");
        migrationBuilder.DropColumn("unit_source_concept_id", "omop_measurement");
    }
}
