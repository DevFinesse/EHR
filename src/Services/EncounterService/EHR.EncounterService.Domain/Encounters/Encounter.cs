using EHR.SharedKernel;

namespace EHR.EncounterService.Domain.Encounters;

public sealed class Encounter
{
    private readonly List<VitalSigns> _vitals = [];
    private readonly List<Diagnosis> _diagnoses = [];

    private Encounter(Guid id, string tenantId, Guid appointmentId, Guid patientId, Guid practitionerId, string visitType)
    {
        Id = id;
        TenantId = tenantId;
        AppointmentId = appointmentId;
        PatientId = patientId;
        PractitionerId = practitionerId;
        VisitType = visitType;
    }

    public Guid Id { get; }
    public string TenantId { get; }
    public Guid AppointmentId { get; }
    public Guid PatientId { get; }
    public Guid PractitionerId { get; }
    public string VisitType { get; }
    public string Status { get; private set; } = "Started";
    public IReadOnlyCollection<VitalSigns> Vitals => _vitals;
    public IReadOnlyCollection<Diagnosis> Diagnoses => _diagnoses;

    public static Encounter Start(string tenantId, Guid appointmentId, Guid patientId, Guid practitionerId, string visitType) =>
        new(Guid.NewGuid(), tenantId.Trim(), appointmentId, patientId, practitionerId, visitType.Trim());

    public static Encounter Restore(
        Guid id,
        string tenantId,
        Guid appointmentId,
        Guid patientId,
        Guid practitionerId,
        string visitType,
        string status,
        IEnumerable<VitalSigns> vitals,
        IEnumerable<Diagnosis> diagnoses)
    {
        var encounter = new Encounter(id, tenantId, appointmentId, patientId, practitionerId, visitType)
        {
            Status = status
        };
        encounter._vitals.AddRange(vitals);
        encounter._diagnoses.AddRange(diagnoses);
        return encounter;
    }

    public void RecordVitals(VitalSigns vitals) => _vitals.Add(vitals);

    public void AddDiagnosis(Diagnosis diagnosis) => _diagnoses.Add(diagnosis);

    public Result<Encounter> Complete()
    {
        if (!_vitals.Any())
        {
            return Result<Encounter>.Failure("At least one vitals record is required before completion.");
        }

        if (!_diagnoses.Any())
        {
            return Result<Encounter>.Failure("At least one diagnosis is required before completion.");
        }

        Status = "Completed";
        return Result<Encounter>.Success(this);
    }
}
