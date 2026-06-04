using EHR.SharedKernel;

namespace EHR.Api.Encounters;

public sealed class Encounter : Entity
{
    private readonly List<VitalSigns> _vitals = [];
    private readonly List<Diagnosis> _diagnoses = [];

    public Encounter(Guid id, string tenantId, Guid appointmentId, Guid patientId, Guid practitionerId, string visitType)
        : base(id)
    {
        TenantId = tenantId;
        AppointmentId = appointmentId;
        PatientId = patientId;
        PractitionerId = practitionerId;
        VisitType = visitType;
    }

    public string TenantId { get; }
    public Guid AppointmentId { get; }
    public Guid PatientId { get; }
    public Guid PractitionerId { get; }
    public string VisitType { get; }
    public string Status { get; private set; } = "Started";
    public IReadOnlyCollection<VitalSigns> Vitals => _vitals;
    public IReadOnlyCollection<Diagnosis> Diagnoses => _diagnoses;

    public void RecordVitals(VitalSigns vitals) => _vitals.Add(vitals);

    public void AddDiagnosis(Diagnosis diagnosis) => _diagnoses.Add(diagnosis);

    public void Complete() => Status = "Completed";
}

public sealed record VitalSigns(decimal TemperatureCelsius, int SystolicBloodPressure, int DiastolicBloodPressure, int PulseRate, int OxygenSaturation);

public sealed record Diagnosis(string Code, string Description, string Certainty);

public sealed record StartEncounterCommand(Guid AppointmentId, string VisitType);

public sealed record RecordVitalsCommand(decimal TemperatureCelsius, int SystolicBloodPressure, int DiastolicBloodPressure, int PulseRate, int OxygenSaturation);

public sealed record AddDiagnosisCommand(string Code, string Description, string Certainty);
