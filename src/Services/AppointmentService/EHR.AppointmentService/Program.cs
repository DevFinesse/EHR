using EHR.AppointmentService.Application.Appointments;
using EHR.AppointmentService.Application.Patients;
using EHR.AppointmentService.Domain.Appointments;
using EHR.AppointmentService.Infrastructure.Appointments;
using EHR.AppointmentService.Infrastructure.Patients;
using EHR.Cqrs;
using EHR.Messaging;
using EHR.ServiceDefaults;
using EHR.SharedKernel;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.AppointmentService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
builder.Services.AddValidatorsFromAssemblyContaining<BookAppointmentCommand>();
var appointmentDb = builder.Configuration.GetConnectionString("AppointmentDb");
if (string.IsNullOrWhiteSpace(appointmentDb))
{
    builder.Services.AddSingleton<IAppointmentRepository>(provider => new InMemoryAppointmentRepository(provider.GetRequiredService<IEventBus>()));
    builder.Services.AddSingleton<IKnownPatientRepository, InMemoryKnownPatientRepository>();
    builder.Services.AddScoped<IQueryHandler<GetAppointmentByIdQuery, Appointment?>, GetAppointmentByIdHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => AppointmentDatabaseMigrator.MigrateAsync(appointmentDb), "Appointment database migration");
    builder.Services.AddSingleton<IInboxStore>(_ => new PostgresInboxStore(appointmentDb));
    builder.Services.AddSingleton<IAppointmentRepository>(provider => new PostgresAppointmentRepository(
        appointmentDb,
        provider.GetRequiredService<IOutboxPublisherSignal>()));
    builder.Services.AddSingleton<IKnownPatientRepository>(_ => new PostgresKnownPatientRepository(appointmentDb));
    builder.Services.AddScoped<IQueryHandler<GetAppointmentByIdQuery, Appointment?>>(provider => new DapperAppointmentQueryHandler(appointmentDb, provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddScoped<IQueryHandler<ListAppointmentsQuery, IReadOnlyCollection<Appointment>>>(provider => new DapperListAppointmentsQueryHandler(
        appointmentDb,
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ICurrentUserContext>(),
        provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddHostedService(provider => new AppointmentOutboxPublisherWorker(
        appointmentDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<IOutboxPublisherSignal>(),
        provider.GetRequiredService<ILogger<AppointmentOutboxPublisherWorker>>()));
}
builder.Services.AddScoped<ICommandHandler<BookAppointmentCommand, Result<Appointment>>, BookAppointmentHandler>();
builder.Services.AddScoped<ICommandHandler<CheckInPatientCommand, Result<Appointment>>, CheckInPatientHandler>();
builder.Services.AddSingleton<IIntegrationEventHandler, PatientRegisteredIntegrationEventHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
