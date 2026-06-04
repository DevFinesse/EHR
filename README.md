# EHR Platform

Cloud-native Electronic Health Record platform MVP for hospitals, clinics, and telemedicine providers across Africa.

## Architecture

The platform is now split into independently deployable service hosts. The original `src/EHR.Api` project is retained as the first modular MVP/reference host, but the microservice path lives under `src/Services`.

## Services

- `src/Services/TenantService/EHR.TenantService` - hospitals, tenant IDs, branches, plans, settings.
- `src/Services/IdentityService/EHR.IdentityService` - staff users, roles, permissions, authentication boundary.
- `src/Services/PatientService/EHR.PatientService` - patient demographics, MRNs, contacts, insurance profile.
- `src/Services/AppointmentService/EHR.AppointmentService` - appointment booking, queue/check-in workflow.
- `src/Services/EncounterService/EHR.EncounterService` - visits, vitals, diagnosis, encounter completion.
- `src/Services/AuditService/EHR.AuditService` - audit events and compliance history.

Each service has its own ASP.NET Core host and service-owned persistence. EF Core-backed PostgreSQL repositories are used when a service connection string is configured; in-memory repositories remain available for lightweight local runs and unit tests.

## Clean Architecture

Each microservice is split into class-library boundaries:

```text
EHR.<Service>.Domain
EHR.<Service>.Application
EHR.<Service>.Infrastructure
EHR.<Service>
```

Dependency direction:

```text
API -> Application
API -> Infrastructure
Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain
Domain -> no service-layer dependency
```

Responsibilities:

- `Domain` - entities, value objects, business behavior, business rules.
- `Application` - commands, queries, CQRS handlers, repository abstractions, use cases.
- `Infrastructure` - repository implementations, event/persistence adapters.
- `API` - controllers, HTTP concerns, dependency injection composition.

Controllers are intentionally thin. They receive HTTP requests, dispatch commands/queries through `ICqrsDispatcher`, and translate results into HTTP responses.

## Custom CQRS

The custom CQRS implementation is in `src/BuildingBlocks/EHR.Cqrs`.

It provides:

- `ICommand<TResponse>`
- `IQuery<TResponse>`
- `ICommandHandler<TCommand, TResponse>`
- `IQueryHandler<TQuery, TResponse>`
- `ICqrsDispatcher`
- `CqrsDispatcher`

Service endpoints do not call application logic directly. They send commands and queries through `ICqrsDispatcher`, and each service registers only the handlers it owns.

## Workflow

1. Register hospital
2. Create staff user
3. Register patient
4. Book appointment
5. Check patient in
6. Start encounter
7. Record vitals
8. Add diagnosis
9. Complete encounter
10. Generate audit events

## Projects

- `src/EHR.Api` - original modular MVP/reference host.
- `src/AppHost/EHR.AppHost` - .NET Aspire AppHost for local orchestration.
- `src/ApiGateway/EHR.ApiGateway` - YARP API Gateway for client-facing routing.
- `src/Services/*` - real service hosts.
- `src/Services/*/*.Domain` - service domain class libraries.
- `src/Services/*/*.Application` - service application class libraries.
- `src/Services/*/*.Infrastructure` - service infrastructure class libraries.
- `src/BuildingBlocks/EHR.Cqrs` - custom CQRS abstractions and dispatcher.
- `src/BuildingBlocks/EHR.SharedKernel` - shared primitives such as entities, results, and tenant context.
- `src/BuildingBlocks/EHR.Messaging` - integration-event contracts, in-memory publishing, Kafka publishing, and Kafka consumer dispatch.
- `src/BuildingBlocks/EHR.ServiceDefaults` - Serilog, OpenTelemetry, JWT, role policies, health checks, Scalar, and infrastructure config loading.
- `tests/EHR.Api.Tests` - behavior tests for the first clinical workflow.

## Infrastructure

Local infrastructure is defined in `docker-compose.yml`:

- Kafka on `localhost:9092` using `confluentinc/cp-kafka:7.6.1`
- Mailpit SMTP on `localhost:1025` and inbox UI on `localhost:8025`
- Tenant PostgreSQL on `localhost:5433`
- Identity PostgreSQL on `localhost:5434`
- Patient PostgreSQL on `localhost:5435`
- Appointment PostgreSQL on `localhost:5436`
- Encounter PostgreSQL on `localhost:5437`
- Audit PostgreSQL on `localhost:5438`

Each service has an `appsettings.Infrastructure.json` file with:

- `Kafka:BootstrapServers`
- `Jwt` issuer/audience/signing key
- service-owned PostgreSQL connection string

`EHR.Messaging` now contains both:

- `InMemoryEventBus` for tests/local runs without Kafka config
- `KafkaEventBus` for Kafka-backed publishing when `Kafka:BootstrapServers` is configured
- `KafkaConsumerWorker` and `IIntegrationEventHandler` for service-level Kafka consumers

`EHR.ServiceDefaults` adds:

- Serilog request logging and console logs
- OpenTelemetry ASP.NET Core and HTTP tracing with console exporter
- OTLP exporting when `OpenTelemetry:OtlpEndpoint` is configured
- `/health`
- JWT bearer validation
- role policies: `ClinicalWrite`, `Admin`

EF Core-backed PostgreSQL repositories are available for:

- Tenant hospitals
- Identity staff users, refresh tokens, staff invitations, and password reset tokens
- Patients and tenant-registration read models
- Appointments and known-patient read models
- Encounters, including JSONB vitals and diagnoses
- Audit records

Each service Infrastructure project owns an EF Core `DbContext` for its PostgreSQL tables. Repositories map between EF row models and Domain entities so Domain remains persistence-ignorant.

Each service runs EF Core migrations on startup when its service-owned connection string is configured. Applied versions are recorded by EF Core in the service database's `__EFMigrationsHistory` table. Without the connection string, the service falls back to in-memory repositories.

EF Core migration files live under each service Infrastructure project:

```text
src/Services/<Service>/EHR.<Service>.Infrastructure/Migrations/<version>_InitialEfCore.cs
```

## Run Services

```powershell
dotnet run --project src/ApiGateway/EHR.ApiGateway --urls http://localhost:5190
dotnet run --project src/Services/TenantService/EHR.TenantService --urls http://localhost:5191
dotnet run --project src/Services/IdentityService/EHR.IdentityService --urls http://localhost:5192
dotnet run --project src/Services/PatientService/EHR.PatientService --urls http://localhost:5193
dotnet run --project src/Services/AppointmentService/EHR.AppointmentService --urls http://localhost:5194
dotnet run --project src/Services/EncounterService/EHR.EncounterService --urls http://localhost:5195
dotnet run --project src/Services/AuditService/EHR.AuditService --urls http://localhost:5196
```

When running with `appsettings.Infrastructure.json`, start the infrastructure first:

```powershell
docker compose up -d kafka tenant-db identity-db patient-db appointment-db encounter-db audit-db jaeger
```

Set the JWT signing key through environment variables or user secrets, not checked-in appsettings:

```powershell
$env:Jwt__SigningKey = "development-signing-key-change-before-production-32chars"
```

Run the full containerized stack:

```powershell
docker compose up --build
```

Run the Aspire AppHost:

```powershell
dotnet run --project src/AppHost/EHR.AppHost
```

The AppHost starts the same local platform graph as Compose:

- Confluent Kafka
- PostgreSQL databases for every service
- Mailpit
- Jaeger
- Tenant, Identity, Patient, Appointment, Encounter, Audit services
- API Gateway

Example command:

```powershell
$body = @{
  name = "Lagos Care Hospital"
  country = "Nigeria"
  city = "Lagos"
  plan = "Growth"
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5191/api/hospitals -Method Post -Body $body -ContentType application/json
```

Gateway route example:

```powershell
Invoke-RestMethod -Uri http://localhost:5190/tenant/api/hospitals -Method Post -Body $body -ContentType application/json
```

Identity token flow:

```powershell
$invite = @{
  tenantId = "tenant-demo"
  fullName = "Dr Ada Okafor"
  email = "ada@example.com"
  role = "Doctor"
  department = "General Medicine"
} | ConvertTo-Json

$invitation = Invoke-RestMethod -Uri http://localhost:5192/api/staff/invitations -Method Post -Body $invite -ContentType application/json

$accept = @{
  invitationToken = $invitation.invitationToken
  password = "Use-a-real-secret-123!"
  mfaCode = $null
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5192/api/auth/invitations/accept -Method Post -Body $accept -ContentType application/json

$login = @{
  email = "ada@example.com"
  password = "Use-a-real-secret-123!"
  mfaCode = $null
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:5192/api/auth/login -Method Post -Body $login -ContentType application/json
```

Development OpenAPI is available at `/openapi/v1.json`; Scalar API reference is available at `/scalar` on each host in Development.

Identity security now includes:

- PBKDF2 password hashing
- Staff invitation tokens
- SMTP staff invitation email delivery
- Password reset tokens and reset flow
- Invitation acceptance with password setup
- Refresh-token rotation
- Failed-login counters
- 15-minute account lockout after 5 failed attempts
- RFC 6238 TOTP MFA provider
- Recovery codes protected with SHA-256 hashes

Configure SMTP through:

```text
Email__From
Email__Smtp__Host
Email__Smtp__Port
Email__Smtp__Username
Email__Smtp__Password
Email__Smtp__EnableSsl
```

Compose uses Mailpit for local SMTP capture.

Authorization is applied to service endpoints:

- `ClinicalWrite` protects patient, appointment, and encounter clinical workflows.
- `Admin` protects staff management and audit event APIs.
- Login, refresh, invitation acceptance, password reset, and tenant onboarding remain public bootstrap/auth endpoints.

Concrete Kafka consumers now include:

- AuditService materializes integration events into audit records.
- PatientService consumes `tenant.hospital.registered` into a tenant registration read model.
- AppointmentService consumes `patient.created` into a known-patient read model.

## Infrastructure Tests

Normal test runs do not require Docker:

```powershell
dotnet test EHR.Platform.slnx
```

To run PostgreSQL and Kafka smoke tests against the Compose infrastructure:

```powershell
docker compose up -d kafka tenant-db identity-db patient-db appointment-db encounter-db audit-db
$env:RUN_INFRA_TESTS = "true"
dotnet test tests/EHR.Infrastructure.Tests
```

To run the containerized API Gateway workflow test, start the full stack first:

```powershell
docker compose up --build
$env:RUN_E2E_TESTS = "true"
$env:GATEWAY_BASE_URL = "http://localhost:5190"
dotnet test tests/EHR.Infrastructure.Tests
```

For the original reference host, send tenant-scoped requests with:

- `X-Tenant-Id`
- `X-Branch-Id`
- `X-User-Id`
- `X-Role`
- `X-Correlation-Id`

The hospital registration endpoint creates the initial tenant ID. Use that value in later workflow calls.

## Next Infrastructure Steps

- Swap SMTP settings from Mailpit to the production email provider.
- Split each initial migration into smaller forward-only migration versions as schema changes grow.
- Add tenant-scoped authorization requirements, not only role policies.
- Promote read-model consumers into process managers for cross-service workflows such as referral, discharge, and billing.
- Add CI secrets for production-like SMTP and observability exporters when those environments exist.
