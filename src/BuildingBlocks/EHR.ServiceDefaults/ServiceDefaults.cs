using System.Text;
using EHR.SharedKernel.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;

namespace EHR.ServiceDefaults;

public static class ServiceDefaults
{
    public static async Task RunWithStartupRetryAsync(Func<Task> action, string operationName, int attempts = 10)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch when (attempt < attempts)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(attempt * 2, 15)));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"{operationName} failed after {attempts} attempts.", exception);
            }
        }
    }

    public static WebApplicationBuilder AddEhrServiceDefaults(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Configuration.AddJsonFile("appsettings.Infrastructure.json", optional: true, reloadOnChange: true);

        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Service", serviceName)
                .WriteTo.Console();
        });

        builder.Services.AddHealthChecks();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContextAccessor>();
        builder.Services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
            };
        });
        builder.Services.AddExceptionHandler<EhrExceptionHandler>();
        builder.Services.Configure<OpenApiOptions>(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Components ??= new OpenApiComponents
                {
                    SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>()
                };
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    Description = "Paste only the JWT access token. Scalar sends it as Authorization: Bearer {token}."
                };

                return Task.CompletedTask;
            });
        });

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });

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

            builder.Services.AddAuthorization(options =>
            {
                foreach (var permission in PlatformPermissions.All)
                {
                    options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
                }

                options.AddPolicy("ClinicalWrite", policy => policy.RequireClaim("permission", PlatformPermissions.EncountersWrite));
                options.AddPolicy("Admin", policy => policy.RequireClaim("permission", PlatformPermissions.StaffManage));
            });
        }
        else
        {
            builder.Services.AddAuthentication();
            builder.Services.AddAuthorization();
        }

        return builder;
    }

    public static WebApplication UseEhrServiceDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
        }

        return app;
    }
}
