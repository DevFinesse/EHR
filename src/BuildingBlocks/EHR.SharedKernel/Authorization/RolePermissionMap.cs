namespace EHR.SharedKernel.Authorization;

public static class RolePermissionMap
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole =
        new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [PlatformRoles.SuperAdmin] = PlatformPermissions.All.ToArray(),
            [PlatformRoles.HospitalAdmin] =
            [
                PlatformPermissions.TenantRead,
                PlatformPermissions.TenantManage,
                PlatformPermissions.StaffRead,
                PlatformPermissions.StaffManage,
                PlatformPermissions.StaffInvite,
                PlatformPermissions.StaffMetadataRead,
                PlatformPermissions.StaffMetadataManage,
                PlatformPermissions.PatientsRead,
                PlatformPermissions.PatientsCreate,
                PlatformPermissions.PatientsUpdate,
                PlatformPermissions.AppointmentsRead,
                PlatformPermissions.AppointmentsBook,
                PlatformPermissions.AppointmentsCheckIn,
                PlatformPermissions.EncountersRead,
                PlatformPermissions.EncountersWrite,
                PlatformPermissions.AuditRead,
                PlatformPermissions.AnalyticsRead,
                PlatformPermissions.AnalyticsManage
            ],
            [PlatformRoles.Doctor] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.AppointmentsRead,
                PlatformPermissions.EncountersRead,
                PlatformPermissions.EncountersWrite
            ],
            [PlatformRoles.Nurse] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.PatientsCreate,
                PlatformPermissions.PatientsUpdate,
                PlatformPermissions.AppointmentsRead,
                PlatformPermissions.AppointmentsCheckIn,
                PlatformPermissions.EncountersRead,
                PlatformPermissions.EncountersWrite
            ],
            [PlatformRoles.Pharmacist] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.EncountersRead
            ],
            [PlatformRoles.LabScientist] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.EncountersRead
            ],
            [PlatformRoles.Radiologist] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.EncountersRead
            ],
            [PlatformRoles.RecordsOfficer] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.PatientsCreate,
                PlatformPermissions.PatientsUpdate,
                PlatformPermissions.AppointmentsRead
            ],
            [PlatformRoles.BillingOfficer] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.AppointmentsRead
            ],
            [PlatformRoles.Receptionist] =
            [
                PlatformPermissions.PatientsRead,
                PlatformPermissions.PatientsCreate,
                PlatformPermissions.PatientsUpdate,
                PlatformPermissions.AppointmentsRead,
                PlatformPermissions.AppointmentsBook
            ],
            [PlatformRoles.Auditor] =
            [
                PlatformPermissions.TenantRead,
                PlatformPermissions.AuditRead,
                PlatformPermissions.AnalyticsRead
            ]
        };

    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> All => PermissionsByRole;

    public static IReadOnlyCollection<string> GetPermissions(string role) =>
        PermissionsByRole.TryGetValue(role, out var permissions) ? permissions : [];
}
