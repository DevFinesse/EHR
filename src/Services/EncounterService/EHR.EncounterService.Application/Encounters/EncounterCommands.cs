using EHR.Cqrs;
using EHR.EncounterService.Domain.Encounters;
using EHR.SharedKernel;

namespace EHR.EncounterService.Application.Encounters;

public sealed record StartEncounterCommand(string TenantId, Guid AppointmentId, Guid PatientId, Guid PractitionerId, string VisitType) : ICommand<Encounter>;

public sealed record RecordVitalsCommand(Guid EncounterId, decimal TemperatureCelsius, int SystolicBloodPressure, int DiastolicBloodPressure, int PulseRate, int OxygenSaturation) : ICommand<Result<Encounter>>;

public sealed record AddDiagnosisCommand(Guid EncounterId, string Code, string Description, string Certainty) : ICommand<Result<Encounter>>;

public sealed record CompleteEncounterCommand(Guid EncounterId) : ICommand<Result<Encounter>>;
