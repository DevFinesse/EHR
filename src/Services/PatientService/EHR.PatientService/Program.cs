using EHR.Cqrs;
using EHR.Messaging;
using EHR.PatientService.Application.Patients;
using EHR.PatientService.Application.Tenants;
using EHR.PatientService.Domain.Patients;
using EHR.PatientService.Infrastructure.Patients;
using EHR.PatientService.Infrastructure.Tenants;
using EHR.ServiceDefaults;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.PatientService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterPatientCommand>();
var patientDb = builder.Configuration.GetConnectionString("PatientDb");
if (string.IsNullOrWhiteSpace(patientDb))
{
    builder.Services.AddSingleton<IPatientRepository>(provider => new InMemoryPatientRepository(provider.GetRequiredService<IEventBus>()));
    builder.Services.AddSingleton<ITenantRegistrationReadModelRepository, InMemoryTenantRegistrationReadModelRepository>();
    builder.Services.AddScoped<IQueryHandler<GetPatientByIdQuery, Patient?>, GetPatientByIdHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => PatientDatabaseMigrator.MigrateAsync(patientDb), "Patient database migration");
    builder.Services.AddSingleton<IInboxStore>(_ => new PostgresInboxStore(patientDb));
    builder.Services.AddScoped<IPatientRepository>(provider => new PostgresPatientRepository(
        patientDb,
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ICurrentUserContext>(),
        provider.GetRequiredService<IOutboxPublisherSignal>()));
    builder.Services.AddSingleton<ITenantRegistrationReadModelRepository>(_ => new PostgresTenantRegistrationReadModelRepository(patientDb));
    builder.Services.AddScoped<IQueryHandler<GetPatientByIdQuery, Patient?>>(provider => new DapperPatientQueryHandler(patientDb, provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddScoped<IQueryHandler<SearchPatientsQuery, IReadOnlyCollection<Patient>>>(provider => new DapperSearchPatientsQueryHandler(
        patientDb,
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ICurrentUserContext>(),
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddHostedService(provider => new PatientOutboxPublisherWorker(
        patientDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<IOutboxPublisherSignal>(),
        provider.GetRequiredService<ILogger<PatientOutboxPublisherWorker>>()));
}
builder.Services.AddScoped<ICommandHandler<RegisterPatientCommand, Patient>, RegisterPatientHandler>();
builder.Services.AddSingleton<IIntegrationEventHandler, TenantRegisteredIntegrationEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
