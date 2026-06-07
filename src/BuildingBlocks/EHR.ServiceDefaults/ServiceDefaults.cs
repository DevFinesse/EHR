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
using OpenTelemetry.Metrics;
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

        var healthChecks = builder.Services.AddHealthChecks();
        AddInfrastructureHealthChecks(builder, healthChecks);
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
        var consoleExporterEnabled = builder.Configuration.GetValue("OpenTelemetry:ConsoleExporterEnabled", false);

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.FilterHttpRequestMessage = request =>
                            !IsTelemetryExporterRequest(request.RequestUri, otlpEndpoint);
                    })
                    .AddSource("EHR.Messaging");

                if (consoleExporterEnabled)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("EHR.Messaging")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                if (consoleExporterEnabled)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
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

    private static void AddInfrastructureHealthChecks(WebApplicationBuilder builder, IHealthChecksBuilder healthChecks)
    {
        foreach (var connectionString in builder.Configuration.GetSection("ConnectionStrings").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(connectionString.Value))
            {
                healthChecks.AddCheck(
                    $"postgres:{connectionString.Key}",
                    new PostgresHealthCheck(connectionString.Value),
                    tags: ["ready", "postgres"]);
            }
        }

        var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
        if (!string.IsNullOrWhiteSpace(kafkaBootstrapServers))
        {
            healthChecks.AddCheck(
                "kafka",
                new KafkaHealthCheck(kafkaBootstrapServers),
                tags: ["ready", "kafka"]);
        }

        var smtpHost = builder.Configuration["Email:Smtp:Host"];
        var smtpPort = builder.Configuration.GetValue<int?>("Email:Smtp:Port");
        if (!string.IsNullOrWhiteSpace(smtpHost) && smtpPort is > 0)
        {
            healthChecks.AddCheck(
                "smtp",
                new SmtpHealthCheck(smtpHost, smtpPort.Value),
                tags: ["ready", "smtp"]);
        }
    }

    private static bool IsTelemetryExporterRequest(Uri? requestUri, string? otlpEndpoint)
    {
        if (requestUri is null || string.IsNullOrWhiteSpace(otlpEndpoint) || !Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            return false;
        }

        return string.Equals(requestUri.Scheme, endpoint.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(requestUri.Host, endpoint.Host, StringComparison.OrdinalIgnoreCase)
            && requestUri.Port == endpoint.Port;
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
