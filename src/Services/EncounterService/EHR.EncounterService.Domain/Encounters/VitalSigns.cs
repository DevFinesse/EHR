namespace EHR.EncounterService.Domain.Encounters;

public sealed record VitalSigns(decimal TemperatureCelsius, int SystolicBloodPressure, int DiastolicBloodPressure, int PulseRate, int OxygenSaturation);
