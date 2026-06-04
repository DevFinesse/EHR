using EHR.Api.Appointments;
using EHR.Api.Encounters;
using EHR.Api.Patients;
using EHR.Api.Staff;
using EHR.Api.Tenants;

namespace EHR.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddEhrHandlers(this IServiceCollection services)
    {
        services.AddScoped<RegisterHospitalHandler>();
        services.AddScoped<CreateStaffUserHandler>();
        services.AddScoped<RegisterPatientHandler>();
        services.AddScoped<BookAppointmentHandler>();
        services.AddScoped<CheckInPatientHandler>();
        services.AddScoped<StartEncounterHandler>();
        services.AddScoped<RecordVitalsHandler>();
        services.AddScoped<AddDiagnosisHandler>();
        services.AddScoped<CompleteEncounterHandler>();
        return services;
    }
}
