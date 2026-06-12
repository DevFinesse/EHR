using System.Diagnostics;
using EHR.Messaging;

namespace EHR.Hl7Api;

public sealed class Hl7AdtWorkflowService
{
    private readonly Hl7PatientWorkflowClient _patients;
    private readonly IEventBus _eventBus;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Hl7AdtWorkflowService(Hl7PatientWorkflowClient patients, IEventBus eventBus, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _patients = patients;
        _eventBus = eventBus;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Hl7AdtWorkflowResult> ApplyAsync(Hl7InboundMessage message, string? tenantId, CancellationToken cancellationToken)
    {
        var resolvedTenantId = ResolveTenantId(tenantId, message);
        var correlationId = Activity.Current?.Id ?? _httpContextAccessor.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");

        await _eventBus.PublishAsync(new Hl7AdtReceivedEvent(
            Guid.NewGuid(),
            resolvedTenantId,
            message.TriggerEvent,
            message.ControlId,
            null,
            message.Patient.MedicalRecordNumber,
            correlationId), cancellationToken);

        var medicalRecordNumber = message.Patient.MedicalRecordNumber;
        if (string.IsNullOrWhiteSpace(medicalRecordNumber))
        {
            throw new Hl7WorkflowException("PID-3 medical record number is required for ADT workflow mapping.");
        }

        var existingPatient = await _patients.FindByMedicalRecordNumberAsync(resolvedTenantId, medicalRecordNumber, cancellationToken);
        var action = message.TriggerEvent switch
        {
            "A01" or "A04" when existingPatient is null => "registered",
            "A01" or "A04" => "matched",
            "A08" when existingPatient is null => throw new Hl7WorkflowException($"Cannot apply ADT^A08 because patient MRN '{medicalRecordNumber}' was not found."),
            "A08" => "updated",
            _ => throw new Hl7WorkflowException($"Unsupported ADT trigger '{message.TriggerEvent}'.")
        };

        var patient = action switch
        {
            "registered" => await _patients.RegisterAsync(resolvedTenantId, message.Patient, cancellationToken),
            "updated" => await _patients.UpdateDemographicsAsync(existingPatient!.Id, message.Patient, cancellationToken),
            _ => existingPatient!
        };

        await _eventBus.PublishAsync(new Hl7AdtPatientMappedEvent(
            Guid.NewGuid(),
            resolvedTenantId,
            message.TriggerEvent,
            message.ControlId,
            patient.Id,
            patient.MedicalRecordNumber,
            action,
            correlationId), cancellationToken);

        return new Hl7AdtWorkflowResult(action, patient.Id, patient.MedicalRecordNumber, resolvedTenantId);
    }

    private string ResolveTenantId(string? tenantId, Hl7InboundMessage message)
    {
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return tenantId.Trim();
        }

        var configuredTenantId = _configuration["Hl7:DefaultTenantId"];
        if (!string.IsNullOrWhiteSpace(configuredTenantId))
        {
            return configuredTenantId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(message.Patient.AssigningAuthority))
        {
            return message.Patient.AssigningAuthority.Trim();
        }

        throw new Hl7WorkflowException("A tenantId query value, Hl7:DefaultTenantId, or PID-3 assigning authority is required.");
    }
}

public sealed record Hl7AdtWorkflowResult(string Action, Guid PatientId, string MedicalRecordNumber, string TenantId);
