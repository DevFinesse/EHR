using System.Collections.Concurrent;
using EHR.Api.Appointments;
using EHR.Api.Audit;
using EHR.Api.Encounters;
using EHR.Api.Patients;
using EHR.Api.Staff;
using EHR.Api.Tenants;

namespace EHR.Api;

public sealed class EhrStore
{
    public ConcurrentDictionary<Guid, Hospital> Hospitals { get; } = new();
    public ConcurrentDictionary<Guid, StaffUser> StaffUsers { get; } = new();
    public ConcurrentDictionary<Guid, Patient> Patients { get; } = new();
    public ConcurrentDictionary<Guid, Appointment> Appointments { get; } = new();
    public ConcurrentDictionary<Guid, Encounter> Encounters { get; } = new();
    public ConcurrentQueue<AuditRecord> AuditRecords { get; } = new();
}
