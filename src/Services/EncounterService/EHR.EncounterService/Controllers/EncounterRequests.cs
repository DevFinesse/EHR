namespace EHR.EncounterService.Controllers;

public sealed record RecordVitalsRequest(decimal TemperatureCelsius, int SystolicBloodPressure, int DiastolicBloodPressure, int PulseRate, int OxygenSaturation);

public sealed record AddDiagnosisRequest(string Code, string Description, string Certainty);
