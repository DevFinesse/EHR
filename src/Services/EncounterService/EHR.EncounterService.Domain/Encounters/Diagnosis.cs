namespace EHR.EncounterService.Domain.Encounters;

public sealed record Diagnosis(string Code, string Description, string Certainty);
