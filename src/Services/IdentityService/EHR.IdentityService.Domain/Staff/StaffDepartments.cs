namespace EHR.IdentityService.Domain.Staff;

public static class StaffDepartments
{
    private static readonly string[] Values =
    [
        "Administration",
        "Billing",
        "Emergency",
        "Inpatient",
        "Internal Medicine",
        "Laboratory",
        "Maternity",
        "Outpatient",
        "Pediatrics",
        "Pharmacy",
        "Platform",
        "Public Health",
        "Radiology",
        "Records",
        "Surgery"
    ];

    public static IReadOnlyCollection<string> All => Values;

    public static bool TryNormalize(string value, out string normalized) =>
        TryNormalize(value, Values, out normalized);

    private static bool TryNormalize(string value, IEnumerable<string> allowedValues, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = allowedValues.FirstOrDefault(allowed => string.Equals(allowed, value.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        normalized = match;
        return true;
    }
}
