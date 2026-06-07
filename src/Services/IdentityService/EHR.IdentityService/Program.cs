using EHR.Cqrs;
using EHR.IdentityService.Application.Auth;
using EHR.IdentityService.Application.Staff;
using EHR.IdentityService.Controllers;
using EHR.IdentityService.Domain.Staff;
using EHR.IdentityService.Infrastructure.Auth;
using EHR.IdentityService.Infrastructure.Staff;
using EHR.IdentityService.Application.Tenants;
using EHR.IdentityService.Infrastructure.Tenants;
using EHR.Messaging;
using EHR.ServiceDefaults;
using EHR.SharedKernel;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.AddEhrServiceDefaults("EHR.IdentityService");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEhrMessaging(builder.Configuration);
builder.Services.AddScoped<ICqrsDispatcher, CqrsDispatcher>();
builder.Services.AddValidatorsFromAssemblyContaining<LoginCommand>();
builder.Services.AddValidatorsFromAssemblyContaining<StaffMetadataItemRequestValidator>();
var identityDb = builder.Configuration.GetConnectionString("IdentityDb");
if (string.IsNullOrWhiteSpace(identityDb))
{
    builder.Services.AddSingleton<IStaffUserRepository>(provider => new InMemoryStaffUserRepository(provider.GetRequiredService<IEventBus>()));
    builder.Services.AddSingleton<ITenantRegistrationReadModelRepository, InMemoryTenantRegistrationReadModelRepository>();
    builder.Services.AddSingleton<IStaffMetadataRepository, InMemoryStaffMetadataRepository>();
    builder.Services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();
    builder.Services.AddSingleton<IStaffInvitationRepository, InMemoryStaffInvitationRepository>();
    builder.Services.AddSingleton<IPasswordResetTokenRepository, InMemoryPasswordResetTokenRepository>();
    builder.Services.AddScoped<IQueryHandler<GetStaffUserByIdQuery, StaffUser?>, GetStaffUserByIdHandler>();
}
else
{
    await ServiceDefaults.RunWithStartupRetryAsync(() => IdentityDatabaseMigrator.MigrateAsync(identityDb), "Identity database migration");
    await ServiceDefaults.RunWithStartupRetryAsync(() => IdentityStaffMetadataSeeder.SeedAsync(identityDb), "Identity staff metadata seed");
    await ServiceDefaults.RunWithStartupRetryAsync(() => IdentityDevelopmentSeeder.SeedAsync(identityDb, new Pbkdf2PasswordHasher(), builder.Configuration, builder.Environment), "Identity development admin seed");
    builder.Services.AddSingleton<IInboxStore>(_ => new PostgresInboxStore(identityDb));
    builder.Services.AddSingleton<IStaffUserRepository>(provider => new PostgresStaffUserRepository(
        identityDb,
        provider.GetRequiredService<IOutboxPublisherSignal>()));
    builder.Services.AddSingleton<ITenantRegistrationReadModelRepository>(_ => new PostgresTenantRegistrationReadModelRepository(identityDb));
    builder.Services.AddSingleton<IStaffMetadataRepository>(_ => new PostgresStaffMetadataRepository(identityDb));
    builder.Services.AddSingleton<IRefreshTokenRepository>(_ => new PostgresRefreshTokenRepository(identityDb));
    builder.Services.AddSingleton<IStaffInvitationRepository>(_ => new PostgresStaffInvitationRepository(identityDb));
    builder.Services.AddSingleton<IPasswordResetTokenRepository>(_ => new PostgresPasswordResetTokenRepository(identityDb));
    builder.Services.AddScoped<IQueryHandler<GetStaffUserByIdQuery, StaffUser?>>(provider => new DapperStaffUserQueryHandler(identityDb, provider.GetRequiredService<EHR.SharedKernel.Authorization.ITenantAuthorizationService>()));
    builder.Services.AddHostedService(provider => new IdentityOutboxPublisherWorker(
        identityDb,
        provider.GetRequiredService<IEventBus>(),
        provider.GetRequiredService<IOutboxPublisherSignal>(),
        provider.GetRequiredService<ILogger<IdentityOutboxPublisherWorker>>()));
}
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITotpProvider, Rfc6238TotpProvider>();
builder.Services.AddSingleton<IRecoveryCodeProtector, RecoveryCodeProtector>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<ICommandHandler<CreateStaffUserCommand, StaffUser>, CreateStaffUserHandler>();
builder.Services.AddSingleton<IIntegrationEventHandler, TenantRegisteredIntegrationEventHandler>();
builder.Services.AddScoped<ICommandHandler<LoginCommand, Result<TokenResponse>>, LoginHandler>();
builder.Services.AddScoped<ICommandHandler<RefreshAccessTokenCommand, Result<TokenResponse>>, RefreshAccessTokenHandler>();
builder.Services.AddScoped<ICommandHandler<InviteStaffCommand, Result<StaffInvitationResponse>>, InviteStaffHandler>();
builder.Services.AddScoped<ICommandHandler<AcceptStaffInvitationCommand, Result<TokenResponse>>, AcceptStaffInvitationHandler>();
builder.Services.AddScoped<ICommandHandler<EnableMfaCommand, Result<bool>>, EnableMfaHandler>();
builder.Services.AddScoped<ICommandHandler<SetupMfaCommand, Result<MfaSetupResponse>>, SetupMfaHandler>();
builder.Services.AddScoped<ICommandHandler<ResetPasswordRequestCommand, Result<PasswordResetResponse>>, ResetPasswordRequestHandler>();
builder.Services.AddScoped<ICommandHandler<ResetPasswordCommand, Result<bool>>, ResetPasswordHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseEhrServiceDefaults();
app.MapControllers();
app.Run();
