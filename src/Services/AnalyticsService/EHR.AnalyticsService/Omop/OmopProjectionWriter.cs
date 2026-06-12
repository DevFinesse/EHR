using System.Text.Json;
using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.AnalyticsService.Omop;

public sealed class OmopProjectionWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DbContextOptions<OmopDbContext> _options;

    public OmopProjectionWriter(string connectionString)
    {
        _options = new DbContextOptionsBuilder<OmopDbContext>().UseNpgsql(connectionString).Options;
    }

    public async Task UpsertPersonAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = Deserialize<PersonPayload>(envelope);
        if (payload is null)
        {
            return;
        }

        await using var db = new OmopDbContext(_options);
        var row = await db.Persons.SingleOrDefaultAsync(person => person.TenantId == envelope.TenantId && person.SourcePatientId == payload.PatientId, cancellationToken);
        if (row is null)
        {
            row = new OmopPersonRow
            {
                PersonId = payload.PatientId,
                TenantId = envelope.TenantId,
                SourcePatientId = payload.PatientId,
                PersonSourceValue = payload.MedicalRecordNumber
            };
            db.Persons.Add(row);
        }

        row.PersonSourceValue = payload.MedicalRecordNumber;
        row.FullName = payload.FullName ?? row.FullName;
        row.BirthDate = payload.DateOfBirth ?? row.BirthDate;
        row.GenderSourceValue = payload.Sex ?? row.GenderSourceValue;
        row.PhoneNumber = payload.PhoneNumber ?? row.PhoneNumber;
        row.UpdatedAt = envelope.OccurredAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertVisitAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = Deserialize<EncounterStartedPayload>(envelope);
        if (payload is null)
        {
            return;
        }

        await using var db = new OmopDbContext(_options);
        var person = await db.Persons.SingleOrDefaultAsync(row => row.TenantId == envelope.TenantId && row.SourcePatientId == payload.PatientId, cancellationToken);
        if (person is null)
        {
            return;
        }

        var visit = await db.VisitOccurrences.SingleOrDefaultAsync(row => row.TenantId == envelope.TenantId && row.SourceEncounterId == payload.EncounterId, cancellationToken);
        if (visit is null)
        {
            visit = new OmopVisitOccurrenceRow
            {
                VisitOccurrenceId = payload.EncounterId,
                TenantId = envelope.TenantId,
                SourceEncounterId = payload.EncounterId,
                PersonId = person.PersonId
            };
            db.VisitOccurrences.Add(visit);
        }

        visit.PersonId = person.PersonId;
        visit.SourceAppointmentId = payload.AppointmentId;
        visit.ProviderId = payload.PractitionerId;
        visit.VisitSourceValue = payload.VisitType;
        visit.VisitStartDateTime = envelope.OccurredAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMeasurementsAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = Deserialize<VitalsRecordedPayload>(envelope);
        if (payload is null)
        {
            return;
        }

        await using var db = new OmopDbContext(_options);
        var visit = await db.VisitOccurrences.SingleOrDefaultAsync(row => row.TenantId == envelope.TenantId && row.SourceEncounterId == payload.EncounterId, cancellationToken);
        if (visit is null)
        {
            return;
        }

        AddMeasurement(db, envelope, visit, "temperature_celsius", payload.TemperatureCelsius, "Cel");
        AddMeasurement(db, envelope, visit, "systolic_blood_pressure", payload.SystolicBloodPressure, "mm[Hg]");
        AddMeasurement(db, envelope, visit, "diastolic_blood_pressure", payload.DiastolicBloodPressure, "mm[Hg]");
        AddMeasurement(db, envelope, visit, "pulse_rate", payload.PulseRate, "/min");
        AddMeasurement(db, envelope, visit, "oxygen_saturation", payload.OxygenSaturation, "%");
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddConditionAsync(EventEnvelope envelope, CancellationToken cancellationToken)
    {
        var payload = Deserialize<DiagnosisAddedPayload>(envelope);
        if (payload is null)
        {
            return;
        }

        await using var db = new OmopDbContext(_options);
        var visit = await db.VisitOccurrences.SingleOrDefaultAsync(row => row.TenantId == envelope.TenantId && row.SourceEncounterId == payload.EncounterId, cancellationToken);
        if (visit is null || await db.ConditionOccurrences.AnyAsync(row => row.SourceEventId == envelope.EventId, cancellationToken))
        {
            return;
        }

        db.ConditionOccurrences.Add(new OmopConditionOccurrenceRow
        {
            ConditionOccurrenceId = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            PersonId = visit.PersonId,
            VisitOccurrenceId = visit.VisitOccurrenceId,
            ConditionSourceValue = payload.Code,
            ConditionConceptId = await StandardConceptIdAsync(db, "Condition", GuessConditionVocabulary(payload.Code), payload.Code, cancellationToken),
            ConditionSourceConceptId = await SourceConceptIdAsync(db, "Condition", GuessConditionVocabulary(payload.Code), payload.Code, cancellationToken),
            ConditionSourceText = payload.Description,
            ConditionStatusSourceValue = payload.Certainty,
            ConditionStartDateTime = envelope.OccurredAt,
            SourceEventId = envelope.EventId
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static void AddMeasurement(OmopDbContext db, EventEnvelope envelope, OmopVisitOccurrenceRow visit, string sourceValue, decimal? value, string unit)
    {
        if (value is null || db.Measurements.Any(row => row.SourceEventId == envelope.EventId && row.MeasurementSourceValue == sourceValue))
        {
            return;
        }

        db.Measurements.Add(new OmopMeasurementRow
        {
            MeasurementId = Guid.NewGuid(),
            TenantId = envelope.TenantId,
            PersonId = visit.PersonId,
            VisitOccurrenceId = visit.VisitOccurrenceId,
            MeasurementSourceValue = sourceValue,
            MeasurementConceptId = StandardConceptId(db, "Measurement", "EHR", sourceValue),
            MeasurementSourceConceptId = SourceConceptId(db, "Measurement", "EHR", sourceValue),
            ValueAsNumber = value.Value,
            UnitSourceValue = unit,
            UnitConceptId = StandardConceptId(db, "Unit", "UCUM", unit),
            UnitSourceConceptId = SourceConceptId(db, "Unit", "UCUM", unit),
            MeasurementDateTime = envelope.OccurredAt,
            SourceEventId = envelope.EventId
        });
    }

    private static T? Deserialize<T>(EventEnvelope envelope)
    {
        var payload = JsonSerializer.Serialize(envelope.Payload, JsonOptions);
        return JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }

    private static long StandardConceptId(OmopDbContext db, string domain, string sourceVocabulary, string sourceCode) =>
        db.ConceptMaps.AsNoTracking()
            .Where(row => row.Domain == domain && row.SourceVocabulary == sourceVocabulary && row.SourceCode == sourceCode)
            .Select(row => row.StandardConceptId)
            .FirstOrDefault();

    private static long SourceConceptId(OmopDbContext db, string domain, string sourceVocabulary, string sourceCode) =>
        db.ConceptMaps.AsNoTracking()
            .Where(row => row.Domain == domain && row.SourceVocabulary == sourceVocabulary && row.SourceCode == sourceCode)
            .Select(row => row.SourceConceptId)
            .FirstOrDefault();

    private static async Task<long> StandardConceptIdAsync(OmopDbContext db, string domain, string sourceVocabulary, string sourceCode, CancellationToken cancellationToken) =>
        await db.ConceptMaps.AsNoTracking()
            .Where(row => row.Domain == domain && row.SourceVocabulary == sourceVocabulary && row.SourceCode == sourceCode)
            .Select(row => row.StandardConceptId)
            .FirstOrDefaultAsync(cancellationToken);

    private static async Task<long> SourceConceptIdAsync(OmopDbContext db, string domain, string sourceVocabulary, string sourceCode, CancellationToken cancellationToken) =>
        await db.ConceptMaps.AsNoTracking()
            .Where(row => row.Domain == domain && row.SourceVocabulary == sourceVocabulary && row.SourceCode == sourceCode)
            .Select(row => row.SourceConceptId)
            .FirstOrDefaultAsync(cancellationToken);

    private static string GuessConditionVocabulary(string code) =>
        code.All(character => char.IsDigit(character) || character == '.') ? "SNOMED" : "ICD10";

    private sealed record PersonPayload(Guid PatientId, string MedicalRecordNumber, string? FullName, DateOnly? DateOfBirth, string? Sex, string? PhoneNumber);
    private sealed record EncounterStartedPayload(Guid EncounterId, Guid PatientId, Guid? AppointmentId, Guid? PractitionerId, string? VisitType);
    private sealed record VitalsRecordedPayload(Guid EncounterId, decimal? TemperatureCelsius, decimal? SystolicBloodPressure, decimal? DiastolicBloodPressure, decimal? PulseRate, decimal? OxygenSaturation);
    private sealed record DiagnosisAddedPayload(Guid EncounterId, string Code, string? Description, string? Certainty);
}

public sealed class OmopPersonProjectionHandler : IIntegrationEventHandler
{
    private readonly OmopProjectionWriter _writer;
    public OmopPersonProjectionHandler(OmopProjectionWriter writer) => _writer = writer;
    public string EventType => "patient.created";
    public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) => _writer.UpsertPersonAsync(envelope, cancellationToken);
}

public sealed class OmopPersonUpdatedProjectionHandler : IIntegrationEventHandler
{
    private readonly OmopProjectionWriter _writer;
    public OmopPersonUpdatedProjectionHandler(OmopProjectionWriter writer) => _writer = writer;
    public string EventType => "patient.demographics_updated";
    public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) => _writer.UpsertPersonAsync(envelope, cancellationToken);
}

public sealed class OmopVisitProjectionHandler : IIntegrationEventHandler
{
    private readonly OmopProjectionWriter _writer;
    public OmopVisitProjectionHandler(OmopProjectionWriter writer) => _writer = writer;
    public string EventType => "encounter.started";
    public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) => _writer.UpsertVisitAsync(envelope, cancellationToken);
}

public sealed class OmopConditionProjectionHandler : IIntegrationEventHandler
{
    private readonly OmopProjectionWriter _writer;
    public OmopConditionProjectionHandler(OmopProjectionWriter writer) => _writer = writer;
    public string EventType => "diagnosis.added";
    public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) => _writer.AddConditionAsync(envelope, cancellationToken);
}

public sealed class OmopMeasurementProjectionHandler : IIntegrationEventHandler
{
    private readonly OmopProjectionWriter _writer;
    public OmopMeasurementProjectionHandler(OmopProjectionWriter writer) => _writer = writer;
    public string EventType => "vitals.recorded";
    public Task HandleAsync(EventEnvelope envelope, CancellationToken cancellationToken) => _writer.AddMeasurementsAsync(envelope, cancellationToken);
}
