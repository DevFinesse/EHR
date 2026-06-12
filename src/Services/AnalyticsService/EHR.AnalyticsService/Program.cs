using EHR.AnalyticsService.Omop;
using EHR.Messaging;
using EHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.AnalyticsService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);

var analyticsDb = builder.Configuration.GetConnectionString("AnalyticsDb");
if (!string.IsNullOrWhiteSpace(analyticsDb))
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => OmopDatabaseMigrator.MigrateAsync(analyticsDb), "OMOP analytics database migration");
    builder.Services.AddSingleton<IInboxStore>(_ => new PostgresInboxStore(analyticsDb));
    builder.Services.AddScoped<OmopProjectionWriter>(_ => new OmopProjectionWriter(analyticsDb));
    builder.Services.AddScoped<OmopQueryService>(_ => new OmopQueryService(analyticsDb));
    builder.Services.AddScoped<OmopConceptMapService>(_ => new OmopConceptMapService(analyticsDb));
    builder.Services.AddScoped<IIntegrationEventHandler, OmopPersonProjectionHandler>();
    builder.Services.AddScoped<IIntegrationEventHandler, OmopPersonUpdatedProjectionHandler>();
    builder.Services.AddScoped<IIntegrationEventHandler, OmopVisitProjectionHandler>();
    builder.Services.AddScoped<IIntegrationEventHandler, OmopConditionProjectionHandler>();
    builder.Services.AddScoped<IIntegrationEventHandler, OmopMeasurementProjectionHandler>();
}
else
{
    builder.Services.AddScoped<OmopQueryService>(_ => new OmopQueryService(null));
    builder.Services.AddScoped<OmopConceptMapService>(_ => new OmopConceptMapService(null));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
