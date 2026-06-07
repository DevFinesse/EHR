using System.Text.Json;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EHR.EncounterService.Infrastructure.Encounters;

public sealed class PostgresEncounterRepository : IEncounterRepository
{
    private readonly DbContextOptions<EncounterDbContext> _options;
    private readonly IOutboxPublisherSignal _outboxSignal;

    public PostgresEncounterRepository(string connectionString, IOutboxPublisherSignal outboxSignal)
    {
        _options = new DbContextOptionsBuilder<EncounterDbContext>().UseNpgsql(connectionString).Options;
        _outboxSignal = outboxSignal;
    }

    public Task AddAsync(Encounter encounter, IntegrationEvent integrationEvent, CancellationToken cancellationToken) =>
        SaveAsync(encounter, integrationEvent, cancellationToken);

    public async Task SaveAsync(Encounter encounter, IntegrationEvent? integrationEvent, CancellationToken cancellationToken)
    {
        await using var db = new EncounterDbContext(_options);
        var row = await db.Encounters.SingleOrDefaultAsync(existing => existing.Id == encounter.Id, cancellationToken);
        if (row is null)
        {
            db.Encounters.Add(new EncounterRow
            {
                Id = encounter.Id,
                TenantId = encounter.TenantId,
                AppointmentId = encounter.AppointmentId,
                PatientId = encounter.PatientId,
                PractitionerId = encounter.PractitionerId,
                VisitType = encounter.VisitType,
                Status = encounter.Status,
                VitalsJson = JsonSerializer.Serialize(encounter.Vitals),
                DiagnosesJson = JsonSerializer.Serialize(encounter.Diagnoses)
            });
        }
        else
        {
            row.Status = encounter.Status;
            row.VitalsJson = JsonSerializer.Serialize(encounter.Vitals);
            row.DiagnosesJson = JsonSerializer.Serialize(encounter.Diagnoses);
        }

        if (integrationEvent is not null)
        {
            db.OutboxMessages.Add(EncounterOutboxMessageRow.FromIntegrationEvent(integrationEvent));
        }

        await db.SaveChangesAsync(cancellationToken);
        if (integrationEvent is not null)
        {
            _outboxSignal.Signal();
        }
    }

    public async Task<Encounter?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = new EncounterDbContext(_options);
        var row = await db.Encounters.AsNoTracking().SingleOrDefaultAsync(encounter => encounter.Id == id, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var vitals = JsonSerializer.Deserialize<VitalSigns[]>(row.VitalsJson) ?? [];
        var diagnoses = JsonSerializer.Deserialize<Diagnosis[]>(row.DiagnosesJson) ?? [];

        return Encounter.Restore(
            row.Id,
            row.TenantId,
            row.AppointmentId,
            row.PatientId,
            row.PractitionerId,
            row.VisitType,
            row.Status,
            vitals,
            diagnoses);
    }
}
