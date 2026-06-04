using EHR.AppointmentService.Application.Patients;
using EHR.AppointmentService.Infrastructure.Appointments;
using Microsoft.EntityFrameworkCore;

namespace EHR.AppointmentService.Infrastructure.Patients;

public sealed class PostgresKnownPatientRepository : IKnownPatientRepository
{
    private readonly DbContextOptions<AppointmentDbContext> _options;

    public PostgresKnownPatientRepository(string connectionString)
    {
        _options = new DbContextOptionsBuilder<AppointmentDbContext>().UseNpgsql(connectionString).Options;
    }

    public async Task UpsertAsync(KnownPatient patient, CancellationToken cancellationToken)
    {
        await using var db = new AppointmentDbContext(_options);
        var row = await db.KnownPatients.SingleOrDefaultAsync(existing => existing.PatientId == patient.PatientId, cancellationToken);
        if (row is null)
        {
            db.KnownPatients.Add(new KnownPatientRow
            {
                PatientId = patient.PatientId,
                TenantId = patient.TenantId,
                MedicalRecordNumber = patient.MedicalRecordNumber,
                RegisteredAt = patient.RegisteredAt,
                CorrelationId = patient.CorrelationId
            });
        }
        else
        {
            row.TenantId = patient.TenantId;
            row.MedicalRecordNumber = patient.MedicalRecordNumber;
            row.RegisteredAt = patient.RegisteredAt;
            row.CorrelationId = patient.CorrelationId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnownPatient?> GetByIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        await using var db = new AppointmentDbContext(_options);
        var row = await db.KnownPatients.AsNoTracking().SingleOrDefaultAsync(patient => patient.PatientId == patientId, cancellationToken);

        return row is null ? null : new KnownPatient(row.PatientId, row.TenantId, row.MedicalRecordNumber, row.RegisteredAt, row.CorrelationId);
    }
}
