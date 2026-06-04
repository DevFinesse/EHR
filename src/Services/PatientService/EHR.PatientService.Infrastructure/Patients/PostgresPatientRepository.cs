using EHR.Messaging;
using EHR.PatientService.Application.Patients;
using EHR.PatientService.Domain.Patients;
using EHR.SharedKernel.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EHR.PatientService.Infrastructure.Patients;

public sealed class PostgresPatientRepository : IPatientRepository
{
    private readonly DbContextOptions<PatientDbContext> _options;
    private readonly ICurrentUserContext _currentUser;

    public PostgresPatientRepository(string connectionString, ICurrentUserContext currentUser)
    {
        _options = new DbContextOptionsBuilder<PatientDbContext>().UseNpgsql(connectionString).Options;
        _currentUser = currentUser;
    }

    public async Task AddAsync(Patient patient, IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = new PatientDbContext(_options, _currentUser);
        if (await db.Patients.AnyAsync(row => row.Id == patient.Id, cancellationToken))
        {
            return;
        }

        db.Patients.Add(new PatientRow
        {
            Id = patient.Id,
            TenantId = patient.TenantId.Value,
            MedicalRecordNumber = patient.MedicalRecordNumber,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            Sex = patient.Sex,
            PhoneNumber = patient.PhoneNumber,
            CreatedAt = patient.CreatedAt
        });

        db.OutboxMessages.Add(OutboxMessageRow.FromIntegrationEvent(integrationEvent));

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = new PatientDbContext(_options, _currentUser);
        var row = await db.Patients.AsNoTracking().SingleOrDefaultAsync(patient => patient.Id == id, cancellationToken);

        return row is null
            ? null
            : Patient.Restore(row.Id, row.TenantId, row.MedicalRecordNumber, row.FullName, row.DateOfBirth, row.Sex, row.PhoneNumber);
    }
}
