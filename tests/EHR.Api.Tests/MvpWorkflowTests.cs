using EHR.Api;
using EHR.Api.Appointments;
using EHR.Api.Audit;
using EHR.Api.Encounters;
using EHR.Api.Patients;
using EHR.Api.Staff;
using EHR.Api.Tenants;
using EHR.Messaging;
using EHR.SharedKernel;

namespace EHR.Api.Tests;

public sealed class MvpWorkflowTests
{
    [Fact]
    public async Task Hospital_to_completed_encounter_workflow_publishes_events_and_audit_records()
    {
        var store = new EhrStore();
        var eventBus = new InMemoryEventBus();
        var audit = new AuditTrail(store, eventBus);
        var context = new TenantContextAccessor
        {
            Current = TenantContext.Platform("test-correlation")
        };

        var hospital = await new RegisterHospitalHandler(store, eventBus, audit)
            .HandleAsync(new RegisterHospitalCommand("Lagos Care Hospital", "Nigeria", "Lagos", "Growth"), context, CancellationToken.None);

        context.Current = new TenantContext(hospital.TenantId, "branch-main", "admin-1", "Hospital Admin", "test-correlation");

        var doctor = await new CreateStaffUserHandler(store, eventBus, audit)
            .HandleAsync(new CreateStaffUserCommand("Dr Ada Okafor", "ada@example.com", "Doctor", "General Medicine"), context, CancellationToken.None);

        var patient = await new RegisterPatientHandler(store, eventBus, audit)
            .HandleAsync(new RegisterPatientCommand("Kemi Bello", new DateOnly(1991, 4, 8), "Female", "+2348011112222"), context, CancellationToken.None);

        var appointment = await new BookAppointmentHandler(store, eventBus, audit)
            .HandleAsync(new BookAppointmentCommand(patient.Id, doctor.Id, DateTimeOffset.UtcNow.AddHours(3), "Fever and fatigue"), context, CancellationToken.None);

        Assert.True(appointment.IsSuccess);

        var checkedIn = await new CheckInPatientHandler(store, eventBus, audit)
            .HandleAsync(appointment.Value!.Id, context, CancellationToken.None);

        var encounter = await new StartEncounterHandler(store, eventBus, audit)
            .HandleAsync(new StartEncounterCommand(checkedIn.Value!.Id, "Outpatient"), context, CancellationToken.None);

        await new RecordVitalsHandler(store, eventBus, audit)
            .HandleAsync(encounter.Value!.Id, new RecordVitalsCommand(37.8m, 120, 80, 88, 98), context, CancellationToken.None);

        await new AddDiagnosisHandler(store, eventBus, audit)
            .HandleAsync(encounter.Value.Id, new AddDiagnosisCommand("R50.9", "Fever, unspecified", "Confirmed"), context, CancellationToken.None);

        var completed = await new CompleteEncounterHandler(store, eventBus, audit)
            .HandleAsync(encounter.Value.Id, context, CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.Equal("Completed", completed.Value!.Status);
        Assert.Contains(eventBus.PublishedEvents, @event => @event.Type == "encounter.completed");
        Assert.Contains(store.AuditRecords, record => record.Action == "EncounterCompleted");
    }
}
