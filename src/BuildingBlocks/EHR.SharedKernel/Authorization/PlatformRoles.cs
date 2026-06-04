namespace EHR.SharedKernel.Authorization;

public static class PlatformRoles
{
    public const string SuperAdmin = "Super Admin";
    public const string HospitalAdmin = "Hospital Admin";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string Pharmacist = "Pharmacist";
    public const string LabScientist = "Lab Scientist";
    public const string Radiologist = "Radiologist";
    public const string RecordsOfficer = "Records Officer";
    public const string BillingOfficer = "Billing Officer";
    public const string Receptionist = "Receptionist";
    public const string Auditor = "Auditor";

    private static readonly string[] Values =
    [
        SuperAdmin,
        HospitalAdmin,
        Doctor,
        Nurse,
        Pharmacist,
        LabScientist,
        Radiologist,
        RecordsOfficer,
        BillingOfficer,
        Receptionist,
        Auditor
    ];

    public static IReadOnlyCollection<string> All => Values;

    public static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = Values.FirstOrDefault(allowed => string.Equals(allowed, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        normalized = match;
        return true;
    }
}
