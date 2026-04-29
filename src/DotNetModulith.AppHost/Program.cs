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
var rustfsAccessKey = builder.AddParameter("rustfs-access-key", "rustfsadmin", publishValueAsDefault: true, secret: false);
var rustfsSecretKey = builder.AddParameter("rustfs-secret-key", "rustfsadmin", publishValueAsDefault: false, secret: true);
const string openObserveUserEmail = "admin@modulith.local";
const string openObserveUserPassword = "Modulith@2026";
var openObserveBasicAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{openObserveUserEmail}:{openObserveUserPassword}"));

var postgres = builder.AddPostgres("postgres", postgresUser, postgresPassword, port: 5432)
    .WithDataVolume("modulith-postgres-data")
    .WithPgAdmin();

var modulithDb = postgres.AddDatabase("modulithdb", "modulith");
var tickerQDb = postgres.AddDatabase("tickerqdb", "tickerq");

var rabbitmq = builder.AddRabbitMQ("rabbitmq", rabbitMqUser, rabbitMqPassword, port: 5672)
    .WithDataVolume("modulith-rabbitmq-data")
    .WithManagementPlugin();

var redis = builder.AddRedis("redis")
    .WithDataVolume("modulith-redis-data")
    .WithRedisInsight();

var rustfs = builder.AddContainer("rustfs", "rustfs/rustfs", "latest")
    .WithHttpEndpoint(port: 9000, targetPort: 9000, name: "s3")
    .WithHttpEndpoint(port: 9001, targetPort: 9001, name: "console")
    .WithVolume("modulith-rustfs-data", "/data")
    .WithEnvironment("AWS_ACCESS_KEY_ID", rustfsAccessKey)
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", rustfsSecretKey)
    .WithEnvironment("MINIO_ROOT_USER", rustfsAccessKey)
    .WithEnvironment("MINIO_ROOT_PASSWORD", rustfsSecretKey)
    .WithEnvironment("RUSTFS_ACCESS_KEY_ID", rustfsAccessKey)
    .WithEnvironment("RUSTFS_SECRET_ACCESS_KEY", rustfsSecretKey);

var oo = builder.AddContainer("openobserve", "public.ecr.aws/zinclabs/openobserve")
    .WithHttpEndpoint(targetPort: 5080, name: "http")
    .WithEnvironment("ZO_ROOT_USER_EMAIL", openObserveUserEmail)
    .WithEnvironment("ZO_ROOT_USER_PASSWORD", openObserveUserPassword)
    .WithEnvironment("ZO_DATA_DIR", "/data")
    .WithVolume("modulith-openobserve-data", "/data");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithHttpEndpoint(targetPort: 4318, name: "otlp-http")
    .WithEndpoint(targetPort: 4317, name: "otlp-grpc", scheme: "grpc")
    .WithBindMount("otel-collector-config.yaml", "/etc/otelcol-contrib/config.yaml")
    .WithEnvironment("OPENOBSERVE_ENDPOINT", oo.GetEndpoint("http"))
    .WithEnvironment("OPENOBSERVE_BASIC_AUTH", openObserveBasicAuth)
    .WithEnvironment("SAMPLING_PERCENTAGE", "10")
    .WaitFor(oo);

var migrations = builder.AddProject<DotNetModulith_MigrationService>("migrations")
    .WithReference(modulithDb)
    .WithReference(tickerQDb)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", openObserveUserEmail)
    .WithEnvironment("OpenObserve__UserPassword", openObserveUserPassword)
    .WaitFor(modulithDb)
    .WaitFor(tickerQDb);

var api = builder.AddProject<DotNetModulith_Api>("api")
    .WithReference(modulithDb)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithEnvironment("Storage__Endpoint", rustfs.GetEndpoint("s3"))
    .WithEnvironment("Storage__AccessKey", rustfsAccessKey)
    .WithEnvironment("Storage__SecretKey", rustfsSecretKey)
    .WithEnvironment("Storage__BucketName", "modulith-files")
    .WithEnvironment("Storage__ForcePathStyle", "true")
    .WithEnvironment("Storage__UseSsl", "false")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", openObserveUserEmail)
    .WithEnvironment("OpenObserve__UserPassword", openObserveUserPassword)
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq)
    .WaitFor(redis)
    .WaitFor(rustfs);

var jobHost = builder.AddProject<DotNetModulith_JobHost>("job")
    .WithReference(modulithDb)
    .WithReference(tickerQDb)
    .WithReference(rabbitmq)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-http"))
    .WithEnvironment("OpenObserve__Enabled", "false")
    .WithEnvironment("OpenObserve__UserEmail", openObserveUserEmail)
    .WithEnvironment("OpenObserve__UserPassword", openObserveUserPassword)
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq);

await builder.Build().RunAsync();
