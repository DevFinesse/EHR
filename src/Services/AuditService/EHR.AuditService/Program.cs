using EHR.AuditService.Application.AuditRecords;
using EHR.AuditService.Domain.AuditRecords;
using EHR.AuditService.Infrastructure.AuditRecords;
using EHR.Cqrs;
using EHR.Messaging;
using EHR.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.AuditService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
var auditDb = builder.Configuration.GetConnectionString("AuditDb");
if (string.IsNullOrWhiteSpace(auditDb))
{
    builder.Services.AddSingleton<IAuditRecordRepository, InMemoryAuditRecordRepository>();
    builder.Services.AddScoped<IQueryHandler<ListAuditRecordsQuery, IReadOnlyCollection<AuditRecord>>, ListAuditRecordsHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => AuditDatabaseMigrator.MigrateAsync(auditDb), "Audit database migration");
    builder.Services.AddSingleton<IAuditRecordRepository>(_ => new PostgresAuditRecordRepository(auditDb));
    builder.Services.AddScoped<IQueryHandler<ListAuditRecordsQuery, IReadOnlyCollection<AuditRecord>>>(provider => new DapperAuditRecordsQueryHandler(
        auditDb,
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ICurrentUserContext>(),
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
}
builder.Services.AddScoped<ICommandHandler<RecordAuditCommand, AuditRecord>, RecordAuditHandler>();
foreach (var eventType in new[]
{
    "tenant.hospital.registered",
    "identity.staff.created",
    "patient.created",
    "appointment.booked",
    "patient.checked_in",
    "encounter.started",
    "vitals.recorded",
    "diagnosis.added",
    "encounter.completed"
})
{
    builder.Services.AddSingleton<IIntegrationEventHandler>(provider =>
        new AuditIntegrationEventHandler(provider.GetRequiredService<IAuditRecordRepository>(), eventType));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
