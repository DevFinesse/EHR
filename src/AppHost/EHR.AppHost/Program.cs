var builder = DistributedApplication.CreateBuilder(args);

const string jwtSigningKey = "development-signing-key-change-before-production-32chars";
const string postgresUser = "ehr";
const string postgresPassword = "ehr_dev_password";

var kafka = builder.AddContainer("kafka", "confluentinc/cp-kafka", "7.6.1")
    .WithEndpoint(port: 9092, targetPort: 29092, name: "external")
    .WithEndpoint(targetPort: 9092, name: "internal")
    .WithEnvironment("CLUSTER_ID", "MkU3OEVBNTcwNTJENDM2Qk")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:9093")
    .WithEnvironment("KAFKA_LISTENERS", "INTERNAL://0.0.0.0:9092,EXTERNAL://0.0.0.0:29092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "INTERNAL://kafka:9092,EXTERNAL://localhost:9092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "INTERNAL:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "INTERNAL")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
    .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.57")
    .WithEndpoint(port: 16686, targetPort: 16686, name: "ui")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp")
    .WithEnvironment("COLLECTOR_OTLP_ENABLED", "true");

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.21")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp")
    .WithEndpoint(port: 8025, targetPort: 8025, name: "ui");

var tenantDb = AddPostgres("tenant-db", "ehr_tenant", 5433);
var identityDb = AddPostgres("identity-db", "ehr_identity", 5434);
var patientDb = AddPostgres("patient-db", "ehr_patient", 5435);
var appointmentDb = AddPostgres("appointment-db", "ehr_appointment", 5436);
var encounterDb = AddPostgres("encounter-db", "ehr_encounter", 5437);
var auditDb = AddPostgres("audit-db", "ehr_audit", 5438);

var tenantService = builder.AddProject<Projects.EHR_TenantService>("tenant-service")
    .WithHttpEndpoint(port: 5191)
    .WaitFor(kafka)
    .WaitFor(tenantDb)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__TenantDb", "Host=localhost;Port=5433;Database=ehr_tenant;Username=ehr;Password=ehr_dev_password");

var identityService = builder.AddProject<Projects.EHR_IdentityService>("identity-service")
    .WithHttpEndpoint(port: 5192)
    .WaitFor(kafka)
    .WaitFor(identityDb)
    .WaitFor(mailpit)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__IdentityDb", "Host=localhost;Port=5434;Database=ehr_identity;Username=ehr;Password=ehr_dev_password")
    .WithEnvironment("Kafka__ConsumerGroupId", "identity-service")
    .WithEnvironment("Kafka__ConsumerTopics__0", "tenant.hospital.registered")
    .WithEnvironment("Email__From", "no-reply@ehr-platform.local")
    .WithEnvironment("Email__Smtp__Host", "localhost")
    .WithEnvironment("Email__Smtp__Port", "1025")
    .WithEnvironment("Email__Smtp__EnableSsl", "false")
    .WithEnvironment("SeedAdmin__Enabled", "true")
    .WithEnvironment("SeedAdmin__Email", "admin@ehr.local")
    .WithEnvironment("SeedAdmin__Password", "Admin123!ChangeMe")
    .WithEnvironment("SeedAdmin__TenantId", "platform")
    .WithEnvironment("SeedAdmin__FullName", "Platform Super Admin")
    .WithEnvironment("SeedAdmin__Role", "Super Admin")
    .WithEnvironment("SeedAdmin__Department", "Platform");

var patientService = builder.AddProject<Projects.EHR_PatientService>("patient-service")
    .WithHttpEndpoint(port: 5193)
    .WaitFor(kafka)
    .WaitFor(patientDb)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__PatientDb", "Host=localhost;Port=5435;Database=ehr_patient;Username=ehr;Password=ehr_dev_password")
    .WithEnvironment("Kafka__ConsumerGroupId", "patient-service")
    .WithEnvironment("Kafka__ConsumerTopics__0", "tenant.hospital.registered");

var appointmentService = builder.AddProject<Projects.EHR_AppointmentService>("appointment-service")
    .WithHttpEndpoint(port: 5194)
    .WaitFor(kafka)
    .WaitFor(appointmentDb)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__AppointmentDb", "Host=localhost;Port=5436;Database=ehr_appointment;Username=ehr;Password=ehr_dev_password")
    .WithEnvironment("Kafka__ConsumerGroupId", "appointment-service")
    .WithEnvironment("Kafka__ConsumerTopics__0", "patient.created");

var encounterService = builder.AddProject<Projects.EHR_EncounterService>("encounter-service")
    .WithHttpEndpoint(port: 5195)
    .WaitFor(kafka)
    .WaitFor(encounterDb)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__EncounterDb", "Host=localhost;Port=5437;Database=ehr_encounter;Username=ehr;Password=ehr_dev_password");

var auditService = builder.AddProject<Projects.EHR_AuditService>("audit-service")
    .WithHttpEndpoint(port: 5196)
    .WaitFor(kafka)
    .WaitFor(auditDb)
    .WithEnvironment("Kafka__BootstrapServers", "localhost:9092")
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ConnectionStrings__AuditDb", "Host=localhost;Port=5438;Database=ehr_audit;Username=ehr;Password=ehr_dev_password")
    .WithEnvironment("Kafka__ConsumerGroupId", "audit-service")
    .WithEnvironment("Kafka__ConsumerTopics__0", "tenant.hospital.registered")
    .WithEnvironment("Kafka__ConsumerTopics__1", "identity.staff.created")
    .WithEnvironment("Kafka__ConsumerTopics__2", "patient.created")
    .WithEnvironment("Kafka__ConsumerTopics__3", "appointment.booked")
    .WithEnvironment("Kafka__ConsumerTopics__4", "patient.checked_in")
    .WithEnvironment("Kafka__ConsumerTopics__5", "encounter.started")
    .WithEnvironment("Kafka__ConsumerTopics__6", "vitals.recorded")
    .WithEnvironment("Kafka__ConsumerTopics__7", "diagnosis.added")
    .WithEnvironment("Kafka__ConsumerTopics__8", "encounter.completed");

builder.AddProject<Projects.EHR_ApiGateway>("api-gateway")
    .WithHttpEndpoint(port: 5190)
    .WaitFor(tenantService)
    .WaitFor(identityService)
    .WaitFor(patientService)
    .WaitFor(appointmentService)
    .WaitFor(encounterService)
    .WaitFor(auditService)
    .WithEnvironment("Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("OpenTelemetry__OtlpEndpoint", "http://localhost:4317")
    .WithEnvironment("ReverseProxy__Clusters__tenant__Destinations__default__Address", "http://localhost:5191/")
    .WithEnvironment("ReverseProxy__Clusters__identity__Destinations__default__Address", "http://localhost:5192/")
    .WithEnvironment("ReverseProxy__Clusters__patient__Destinations__default__Address", "http://localhost:5193/")
    .WithEnvironment("ReverseProxy__Clusters__appointment__Destinations__default__Address", "http://localhost:5194/")
    .WithEnvironment("ReverseProxy__Clusters__encounter__Destinations__default__Address", "http://localhost:5195/")
    .WithEnvironment("ReverseProxy__Clusters__audit__Destinations__default__Address", "http://localhost:5196/");

builder.Build().Run();

IResourceBuilder<ContainerResource> AddPostgres(string name, string database, int port) =>
    builder.AddContainer(name, "postgres", "16")
        .WithEndpoint(port: port, targetPort: 5432, name: "postgres")
        .WithEnvironment("POSTGRES_DB", database)
        .WithEnvironment("POSTGRES_USER", postgresUser)
        .WithEnvironment("POSTGRES_PASSWORD", postgresPassword);
