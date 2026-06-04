using EHR.SharedKernel;

namespace EHR.Api.Patients;

public sealed class Patient : Entity
{
    public Patient(Guid id, string tenantId, string medicalRecordNumber, string fullName, DateOnly dateOfBirth, string sex, string phoneNumber)
        : base(id)
    {
        TenantId = tenantId;
        MedicalRecordNumber = medicalRecordNumber;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        Sex = sex;
        PhoneNumber = phoneNumber;
    }

    public string TenantId { get; }
    public string MedicalRecordNumber { get; }
    public string FullName { get; }
    public DateOnly DateOfBirth { get; }
    public string Sex { get; }
    public string PhoneNumber { get; }
}

public sealed record RegisterPatientCommand(string FullName, DateOnly DateOfBirth, string Sex, string PhoneNumber);
