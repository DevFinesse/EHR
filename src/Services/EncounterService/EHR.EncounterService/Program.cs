using EHR.Cqrs;
using EHR.EncounterService.Application.Encounters;
using EHR.EncounterService.Domain.Encounters;
using EHR.EncounterService.Infrastructure.Encounters;
using EHR.Messaging;
using EHR.ServiceDefaults;
using EHR.SharedKernel;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.EncounterService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
var encounterDb = builder.Configuration.GetConnectionString("EncounterDb");
if (string.IsNullOrWhiteSpace(encounterDb))
{
    builder.Services.AddSingleton<IEncounterRepository>(provider => new InMemoryEncounterRepository(provider.GetRequiredService<IEventBus>()));
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => EncounterDatabaseMigrator.MigrateAsync(encounterDb), "Encounter database migration");
    builder.Services.AddSingleton<IEncounterRepository>(_ => new PostgresEncounterRepository(encounterDb));
    builder.Services.AddHostedService(provider => new EncounterOutboxPublisherWorker(
        encounterDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<ILogger<EncounterOutboxPublisherWorker>>()));
}
builder.Services.AddScoped<ICommandHandler<StartEncounterCommand, Encounter>, StartEncounterHandler>();
builder.Services.AddScoped<ICommandHandler<RecordVitalsCommand, Result<Encounter>>, RecordVitalsHandler>();
builder.Services.AddScoped<ICommandHandler<AddDiagnosisCommand, Result<Encounter>>, AddDiagnosisHandler>();
builder.Services.AddScoped<ICommandHandler<CompleteEncounterCommand, Result<Encounter>>, CompleteEncounterHandler>();
builder.Services.AddScoped<IQueryHandler<GetEncounterByIdQuery, Encounter?>, GetEncounterByIdHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
