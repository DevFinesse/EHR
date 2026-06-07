using EHR.Cqrs;
using EHR.Messaging;
using EHR.ServiceDefaults;
using EHR.TenantService.Application.Hospitals;
using EHR.TenantService.Domain.Hospitals;
using EHR.TenantService.Infrastructure.Hospitals;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.TenantService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
var tenantDb = builder.Configuration.GetConnectionString("TenantDb");
if (string.IsNullOrWhiteSpace(tenantDb))
{
    builder.Services.AddSingleton<IHospitalRepository>(provider => new InMemoryHospitalRepository(provider.GetRequiredService<IEventBus>()));
    builder.Services.AddScoped<IQueryHandler<GetHospitalByIdQuery, Hospital?>, GetHospitalByIdHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => TenantDatabaseMigrator.MigrateAsync(tenantDb), "Tenant database migration");
    builder.Services.AddSingleton<IHospitalRepository>(provider => new PostgresHospitalRepository(
        tenantDb,
        provider.GetRequiredService<IOutboxPublisherSignal>()));
    builder.Services.AddScoped<IQueryHandler<GetHospitalByIdQuery, Hospital?>>(_ => new DapperHospitalQueryHandler(tenantDb));
    builder.Services.AddHostedService(provider => new TenantOutboxPublisherWorker(
        tenantDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<IOutboxPublisherSignal>(),
        provider.GetRequiredService<ILogger<TenantOutboxPublisherWorker>>()));
}
builder.Services.AddScoped<ICommandHandler<RegisterHospitalCommand, Hospital>, RegisterHospitalHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
