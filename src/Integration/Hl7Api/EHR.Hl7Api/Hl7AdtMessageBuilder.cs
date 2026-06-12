using System.Globalization;

namespace EHR.Hl7Api;

public sealed class Hl7AdtMessageBuilder
{
    private static readonly HashSet<string> SupportedTriggerEvents = ["A01", "A04", "A08"];

    public string Build(BuildAdtMessageRequest request)
    {
        var triggerEvent = request.TriggerEvent.Trim().ToUpperInvariant();
        if (!SupportedTriggerEvents.Contains(triggerEvent))
        {
            throw new Hl7ParseException($"Unsupported ADT trigger '{request.TriggerEvent}'. Supported triggers are A01, A04, and A08.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var patientIdentifier = Escape(request.Patient.MedicalRecordNumber) + "^^^" + Escape(request.Patient.AssigningAuthority);
        var patientName = Escape(request.Patient.FamilyName) + "^" + Escape(request.Patient.GivenName);
        var visit = request.Visit;

        var segments = new[]
        {
            $"MSH|^~\\&|{Escape(request.SendingApplication)}|{Escape(request.SendingFacility)}|{Escape(request.ReceivingApplication)}|{Escape(request.ReceivingFacility)}|{timestamp}||ADT^{triggerEvent}|{Escape(request.MessageControlId)}|P|2.5.1",
            $"EVN|{triggerEvent}|{timestamp}",
            $"PID|1|{Escape(request.Patient.ExternalPatientId)}|{patientIdentifier}||{patientName}||{FormatDate(request.Patient.DateOfBirth)}|{Escape(request.Patient.Sex)}|||{Escape(request.Patient.Address)}||{Escape(request.Patient.PhoneNumber)}",
            $"PV1|1|{Escape(visit?.PatientClass)}|{Escape(visit?.AssignedLocation)}||||{Escape(visit?.AttendingDoctorId)}^{Escape(visit?.AttendingDoctorName)}||||||||||||{Escape(visit?.VisitNumber)}|||||||||||||||||||||||||{FormatDateTime(visit?.AdmitDateTime)}"
        };

        return string.Join('\r', segments) + '\r';
    }

    public string BuildAck(Hl7InboundMessage message, string acknowledgmentCode = "AA", string? text = null)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var ackControlId = "ACK-" + Guid.NewGuid().ToString("N");
        var segments = new[]
        {
            $"MSH|^~\\&|{Escape(message.ReceivingApplication)}|{Escape(message.ReceivingFacility)}|{Escape(message.SendingApplication)}|{Escape(message.SendingFacility)}|{timestamp}||ACK^{message.TriggerEvent}|{ackControlId}|P|{message.Version}",
            $"MSA|{acknowledgmentCode}|{Escape(message.ControlId)}|{Escape(text ?? "Message accepted")}"
        };

        return string.Join('\r', segments) + '\r';
    }

    public string BuildApplicationErrorAck(string controlId, string text)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var segments = new[]
        {
            $"MSH|^~\\&|EHR|EHR_PLATFORM|UNKNOWN|UNKNOWN|{timestamp}||ACK|ACK-{Guid.NewGuid():N}|P|2.5.1",
            $"MSA|AE|{Escape(controlId)}|{Escape(text)}"
        };

        return string.Join('\r', segments) + '\r';
    }

    private static string FormatDate(DateOnly? value) => value?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatDateTime(DateTimeOffset? value) => value?.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace(@"\", @"\E\", StringComparison.Ordinal)
            .Replace("|", @"\F\", StringComparison.Ordinal)
            .Replace("^", @"\S\", StringComparison.Ordinal)
            .Replace("&", @"\T\", StringComparison.Ordinal)
            .Replace("~", @"\R\", StringComparison.Ordinal)
            .Trim();
}
