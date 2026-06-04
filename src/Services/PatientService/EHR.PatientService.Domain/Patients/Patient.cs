using EHR.SharedKernel;

namespace EHR.PatientService.Domain.Patients;

public sealed class Patient
{
    private Patient(Guid id, TenantId tenantId, string medicalRecordNumber, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber)
    {
        Id = id;
        TenantId = tenantId;
        MedicalRecordNumber = medicalRecordNumber;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Sex = sex;
        PhoneNumber = phoneNumber;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; }
    public TenantId TenantId { get; }
    public string MedicalRecordNumber { get; }
    public string FullName { get; }
    public DateOnly DateOfBirth { get; }
    public string Sex { get; }
    public string PhoneNumber { get; }
    public DateTimeOffset CreatedAt { get; }

    public static Patient Register(string tenantId, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber) =>
        Register(TenantId.From(tenantId), fullName, dateOfBirth, sex, phoneNumber);

    public static Patient Register(TenantId tenantId, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber) =>
        new(Guid.NewGuid(), tenantId, $"MRN-{DateTimeOffset.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 999999)}", fullName.Trim(), dateOfBirth, sex.Trim(), phoneNumber.Trim());

    public static Patient Restore(Guid id, string tenantId, string medicalRecordNumber, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber) =>
        Restore(id, TenantId.From(tenantId), medicalRecordNumber, fullName, dateOfBirth, sex, phoneNumber);

    public static Patient Restore(Guid id, TenantId tenantId, string medicalRecordNumber, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber) =>
        new(id, tenantId, medicalRecordNumber, fullName, dateOfBirth, sex, phoneNumber);
}
