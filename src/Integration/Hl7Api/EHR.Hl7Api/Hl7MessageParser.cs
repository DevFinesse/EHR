using System.Globalization;

namespace EHR.Hl7Api;

public sealed class Hl7MessageParser
{
    private static readonly HashSet<string> SupportedTriggerEvents = ["A01", "A04", "A08"];

    public Hl7InboundMessage Parse(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage))
        {
            throw new Hl7ParseException("HL7 message body is required.");
        }

        var normalized = rawMessage.Replace("\r\n", "\r", StringComparison.Ordinal).Replace('\n', '\r');
        var segments = normalized.Split('\r', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Split('|'))
            .ToArray();

        var msh = RequiredSegment(segments, "MSH");
        var pid = RequiredSegment(segments, "PID");
        var pv1 = segments.FirstOrDefault(segment => SegmentName(segment) == "PV1");

        var messageType = Component(Field(msh, 9), 1);
        var triggerEvent = Component(Field(msh, 9), 2);
        if (!string.Equals(messageType, "ADT", StringComparison.OrdinalIgnoreCase) || !SupportedTriggerEvents.Contains(triggerEvent))
        {
            throw new Hl7ParseException($"Unsupported HL7 message type '{messageType}^{triggerEvent}'. Supported ADT triggers are A01, A04, and A08.");
        }

        return new Hl7InboundMessage(
            rawMessage,
            messageType,
            triggerEvent,
            Field(msh, 10),
            Field(msh, 3),
            Field(msh, 4),
            Field(msh, 5),
            Field(msh, 6),
            Field(msh, 12),
            ParsePatient(pid),
            pv1 is null ? null : ParseVisit(pv1));
    }

    private static Hl7Patient ParsePatient(string[] pid)
    {
        var patientIdentifier = FirstRepetition(Field(pid, 3));
        return new Hl7Patient(
            Field(pid, 2),
            Component(patientIdentifier, 1),
            Component(patientIdentifier, 4),
            Component(Field(pid, 5), 1),
            Component(Field(pid, 5), 2),
            ParseDate(Field(pid, 7)),
            Field(pid, 8),
            Field(pid, 13),
            Field(pid, 11));
    }

    private static Hl7Visit ParseVisit(string[] pv1)
    {
        var doctor = Field(pv1, 7);
        return new Hl7Visit(
            Field(pv1, 2),
            Field(pv1, 3),
            Component(doctor, 1),
            string.Join(' ', new[] { Component(doctor, 3), Component(doctor, 2) }.Where(value => !string.IsNullOrWhiteSpace(value))),
            Component(Field(pv1, 19), 1),
            ParseDateTime(Field(pv1, 44)));
    }

    private static string[] RequiredSegment(string[][] segments, string name) =>
        segments.FirstOrDefault(segment => SegmentName(segment) == name) ?? throw new Hl7ParseException($"{name} segment is required.");

    private static string SegmentName(string[] segment) => segment.Length > 0 ? segment[0] : string.Empty;

    private static string Field(string[] segment, int hl7Field)
    {
        var index = SegmentName(segment) == "MSH" && hl7Field > 1 ? hl7Field - 1 : hl7Field;
        return index >= 0 && index < segment.Length ? segment[index] : string.Empty;
    }

    private static string Component(string value, int component)
    {
        var parts = value.Split('^');
        var index = component - 1;
        return index >= 0 && index < parts.Length ? parts[index] : string.Empty;
    }

    private static string FirstRepetition(string value) => value.Split('~', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    private static DateOnly? ParseDate(string value) =>
        DateOnly.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

    private static DateTimeOffset? ParseDateTime(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "yyyyMMddHHmmsszzz", "yyyyMMddHHmmzzz", "yyyyMMddHHmmss", "yyyyMMddHHmm" };
        return DateTimeOffset.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }
}

public sealed class Hl7ParseException : Exception
{
    public Hl7ParseException(string message) : base(message)
    {
    }
}
