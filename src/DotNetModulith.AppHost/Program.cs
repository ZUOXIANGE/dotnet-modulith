using Projects;

// 某些本地环境下 Aspire Dashboard 所需环境变量未注入，会导致 AppHost 启动失败。
// 这里提供默认值兜底，保证 `dotnet run --project AppHost` 可以直接启动。
Environment.SetEnvironmentVariable(
    "ASPNETCORE_URLS",
    Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:15000");
Environment.SetEnvironmentVariable(
    "ASPIRE_ALLOW_UNSECURED_TRANSPORT",
    Environment.GetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT") ?? "true");
Environment.SetEnvironmentVariable(
    "ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL",
    Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL") ?? "http://localhost:18889");

var builder = DistributedApplication.CreateBuilder(args);

var postgresUser = builder.AddParameter("postgres-username", "postgres", publishValueAsDefault: true, secret: false);
var postgresPassword = builder.AddParameter("postgres-password", "postgres", publishValueAsDefault: false, secret: true);
var rabbitMqUser = builder.AddParameter("rabbitmq-username", "guest", publishValueAsDefault: true, secret: false);
var rabbitMqPassword = builder.AddParameter("rabbitmq-password", "guest", publishValueAsDefault: false, secret: true);

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword, port: 5432)
    .WithDataVolume("modulith-postgres-data-v1")
    .WithPgAdmin();

var modulithDb = postgres.AddDatabase("modulithdb", "modulith");
var tickerQDb = postgres.AddDatabase("tickerqdb", "tickerq");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitMqUser, rabbitMqPassword, port: 5672)
    .WithDataVolume("modulith-rabbitmq-data-v1")
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
    .WithReference(tickerQDb)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", "admin@modulith.local")
    .WithEnvironment("OpenObserve__UserPassword", "Modulith@2026")
    .WaitFor(modulithDb)
    .WaitFor(tickerQDb);

var api = builder.AddProject<DotNetModulith_Api>("api")
    .WithReference(modulithDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", "admin@modulith.local")
    .WithEnvironment("OpenObserve__UserPassword", "Modulith@2026")
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq)
    .WaitFor(redis);

var jobhost = builder.AddProject<DotNetModulith_JobHost>("jobhost")
    .WithReference(modulithDb)
    .WithReference(tickerQDb)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", "admin@modulith.local")
    .WithEnvironment("OpenObserve__UserPassword", "Modulith@2026")
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq);

builder.Build().Run();
