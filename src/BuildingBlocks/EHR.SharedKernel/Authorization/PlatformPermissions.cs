namespace EHR.SharedKernel.Authorization;

public static class PlatformPermissions
{
    public const string TenantRead = "tenants.read";
    public const string TenantManage = "tenants.manage";
    public const string StaffRead = "staff.read";
    public const string StaffManage = "staff.manage";
    public const string StaffInvite = "staff.invite";
    public const string StaffMetadataRead = "staff.metadata.read";
    public const string StaffMetadataManage = "staff.metadata.manage";
    public const string PatientsRead = "patients.read";
    public const string PatientsCreate = "patients.create";
    public const string PatientsUpdate = "patients.update";
    public const string AppointmentsRead = "appointments.read";
    public const string AppointmentsBook = "appointments.book";
    public const string AppointmentsCheckIn = "appointments.check_in";
    public const string EncountersRead = "encounters.read";
    public const string EncountersWrite = "encounters.write";
    public const string AuditRead = "audit.read";
    public const string AuditWrite = "audit.write";
    public const string AnalyticsRead = "analytics.read";
    public const string AnalyticsManage = "analytics.manage";

    private static readonly string[] Values =
    [
        TenantRead,
        TenantManage,
        StaffRead,
        StaffManage,
        StaffInvite,
        StaffMetadataRead,
        StaffMetadataManage,
        PatientsRead,
        PatientsCreate,
        PatientsUpdate,
        AppointmentsRead,
        AppointmentsBook,
        AppointmentsCheckIn,
        EncountersRead,
        EncountersWrite,
        AuditRead,
        AuditWrite,
        AnalyticsRead,
        AnalyticsManage
    ];

    public static IReadOnlyCollection<string> All => Values;
}
