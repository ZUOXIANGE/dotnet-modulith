using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("modulith-postgres-data")
    .WithPgAdmin();

var modulithDb = postgres.AddDatabase("modulithdb", "modulith");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("modulith-rabbitmq-data")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithDataVolume("modulith-redis-data")
    .WithRedisInsight();

var openobserve = builder.AddContainer("openobserve", "public.ecr.aws/zinclabs/openobserve")
    .WithHttpEndpoint(targetPort: 5080, name: "http")
    .WithEnvironment("ZO_ROOT_USER_EMAIL", "admin@modulith.local")
    .WithEnvironment("ZO_ROOT_USER_PASSWORD", "Modulith@2026")
    .WithEnvironment("ZO_DATA_DIR", "/data")
    .WithVolume("modulith-openobserve-data", "/data");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "grpc")
    .WithBindMount("otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithEnvironment("OPENOBSERVE_ENDPOINT", openobserve.GetEndpoint("http"))
    .WithEnvironment("OPENOBSERVE_BASIC_AUTH", "YWRtaW5AbW9kdWxpdGgubG9jYWw6TW9kdWxpdGhAMjAyNg==")
    .WithEnvironment("SAMPLING_PERCENTAGE", "10")
    .WaitFor(openobserve);

var migrations = builder.AddProject<DotNetModulith_MigrationService>("migrations")
    .WithReference(modulithDb)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WaitFor(modulithDb);

var api = builder.AddProject<DotNetModulith_Api>("api")
    .WithReference(modulithDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq)
    .WaitFor(redis);

builder.Build().Run();
