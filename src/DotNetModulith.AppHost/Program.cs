using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("modulith-postgres-data")
    .WithPgAdmin();

var ordersDb = postgres.AddDatabase("ordersdb", "modulith_orders");
var inventoryDb = postgres.AddDatabase("inventorydb", "modulith_inventory");
var paymentsDb = postgres.AddDatabase("paymentsdb", "modulith_payments");
var capDb = postgres.AddDatabase("capdb", "modulith_cap");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume("modulith-rabbitmq-data")
    .WithManagementPlugin();

var migrations = builder.AddProject<DotNetModulith_MigrationService>("migrations")
    .WithReference(ordersDb)
    .WithReference(inventoryDb)
    .WithReference(paymentsDb)
    .WithReference(capDb)
    .WaitFor(ordersDb)
    .WaitFor(inventoryDb)
    .WaitFor(paymentsDb)
    .WaitFor(capDb);

var api = builder.AddProject<DotNetModulith_Api>("api")
    .WithReference(ordersDb)
    .WithReference(inventoryDb)
    .WithReference(paymentsDb)
    .WithReference(capDb)
    .WithReference(rabbitmq)
    .WaitForCompletion(migrations)
    .WaitFor(rabbitmq);

builder.Build().Run();
