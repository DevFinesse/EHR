using EHR.Api;
using EHR.Api.Appointments;
using EHR.Api.Audit;
using EHR.Api.Encounters;
using EHR.Api.Patients;
using EHR.Api.Staff;
using EHR.Api.Tenants;
using EHR.Messaging;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<EhrStore>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<AuditTrail>();
builder.Services.AddScoped<TenantContextAccessor>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEhrHandlers();

var signingKey = builder.Configuration["Jwt:SigningKey"];
if (!string.IsNullOrWhiteSpace(signingKey))
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            };
        });
}

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!string.IsNullOrWhiteSpace(signingKey))
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.Use(async (context, next) =>
{
    var accessor = context.RequestServices.GetRequiredService<TenantContextAccessor>();
    accessor.Current = TenantContextAccessor.FromHttpContext(context);
    context.Response.Headers.TryAdd("X-Correlation-Id", accessor.Current.CorrelationId);
    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    name = "EHR Platform",
    version = "0.1.0",
    services = new[]
    {
        "Tenant", "Staff", "Patient", "Appointment", "Encounter", "Audit"
    },
    workflow = "register-hospital -> create-staff -> register-patient -> book-appointment -> check-in -> start-encounter -> record-vitals -> add-diagnosis -> complete-encounter"
}));

app.MapTenantEndpoints();
app.MapStaffEndpoints();
app.MapPatientEndpoints();
app.MapAppointmentEndpoints();
app.MapEncounterEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program;
