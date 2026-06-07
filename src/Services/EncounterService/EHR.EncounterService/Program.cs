using EHR.Cqrs;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.EncounterService.Infrastructure.Encounters;
using EHR.Messaging;
using EHR.ServiceDefaults;
using EHR.SharedKernel;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.EncounterService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
builder.Services.AddValidatorsFromAssemblyContaining<StartEncounterCommand>();
var encounterDb = builder.Configuration.GetConnectionString("EncounterDb");
if (string.IsNullOrWhiteSpace(encounterDb))
{
    builder.Services.AddSingleton<IEncounterRepository>(provider => new InMemoryEncounterRepository(provider.GetRequiredService<IEventBus>()));
    builder.Services.AddScoped<IQueryHandler<GetEncounterByIdQuery, Encounter?>, GetEncounterByIdHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => EncounterDatabaseMigrator.MigrateAsync(encounterDb), "Encounter database migration");
    builder.Services.AddSingleton<IEncounterRepository>(provider => new PostgresEncounterRepository(
        encounterDb,
        provider.GetRequiredService<IOutboxPublisherSignal>()));
    builder.Services.AddScoped<IQueryHandler<GetEncounterByIdQuery, Encounter?>>(provider => new DapperEncounterQueryHandler(encounterDb, provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddScoped<IQueryHandler<ListEncountersQuery, IReadOnlyCollection<Encounter>>>(provider => new DapperListEncountersQueryHandler(
        encounterDb,
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ICurrentUserContext>(),
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddHostedService(provider => new EncounterOutboxPublisherWorker(
        encounterDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<IOutboxPublisherSignal>(),
        provider.GetRequiredService<ILogger<EncounterOutboxPublisherWorker>>()));
}
builder.Services.AddScoped<ICommandHandler<StartEncounterCommand, Encounter>, StartEncounterHandler>();
builder.Services.AddScoped<ICommandHandler<RecordVitalsCommand, Result<Encounter>>, RecordVitalsHandler>();
builder.Services.AddScoped<ICommandHandler<AddDiagnosisCommand, Result<Encounter>>, AddDiagnosisHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteEncounterCommand, Result<Encounter>>, CompleteEncounterHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
