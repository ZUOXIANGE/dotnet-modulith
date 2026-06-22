using DotNetModulith.JobHost.Infrastructure;
using DotNetModulith.Modules.Books.Infrastructure;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using DotNetModulith.Modules.Fines.Infrastructure;
using DotNetModulith.Modules.Members.Infrastructure;
using DotNetModulith.Modules.Notifications.Infrastructure;
using DotNetModulith.Modules.Reservation.Infrastructure;
using DotNetModulith.Modules.Storage.Infrastructure;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("modulithdb")
    ?? throw new InvalidOperationException("modulithdb connection string not found.");
var tickerQConnectionString = builder.Configuration.GetConnectionString("tickerqdb")
    ?? throw new InvalidOperationException("tickerqdb connection string not found.");

builder.Services.AddDbContext<UsersDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName)));
builder.Services.AddDbContext<BooksDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(BooksDbContext).Assembly.FullName)));
builder.Services.AddDbContext<MembersDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(MembersDbContext).Assembly.FullName)));
builder.Services.AddDbContext<BorrowingDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(BorrowingDbContext).Assembly.FullName)));
builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(ReservationDbContext).Assembly.FullName)));
builder.Services.AddDbContext<FinesDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(FinesDbContext).Assembly.FullName)));
builder.Services.AddDbContext<NotificationsDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(NotificationsDbContext).Assembly.FullName)));
builder.Services.AddDbContext<StorageDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(StorageDbContext).Assembly.FullName)));
builder.Services.AddDbContext<TickerQSchedulerDbContext>(options =>
    options.UseNpgsql(
        tickerQConnectionString,
        npgsql => npgsql.MigrationsAssembly(typeof(TickerQSchedulerDbContext).Assembly.FullName)));

builder.Services.AddHostedService<MigrationWorker>();

var host = builder.Build();
host.Run();

internal sealed class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MigrationWorker> _logger;

    public MigrationWorker(
        IServiceProvider serviceProvider,
        IHostApplicationLifetime hostApplicationLifetime,
        ILogger<MigrationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Migration service starting for {Application}", "DotNetModulith");

        try
        {
            using var scope = _serviceProvider.CreateScope();

            var dbContexts = new Dictionary<string, DbContext>
            {
                ["Users"] = scope.ServiceProvider.GetRequiredService<UsersDbContext>(),
                ["Books"] = scope.ServiceProvider.GetRequiredService<BooksDbContext>(),
                ["Members"] = scope.ServiceProvider.GetRequiredService<MembersDbContext>(),
                ["Borrowing"] = scope.ServiceProvider.GetRequiredService<BorrowingDbContext>(),
                ["Reservation"] = scope.ServiceProvider.GetRequiredService<ReservationDbContext>(),
                ["Fines"] = scope.ServiceProvider.GetRequiredService<FinesDbContext>(),
                ["Notifications"] = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>(),
                ["Storage"] = scope.ServiceProvider.GetRequiredService<StorageDbContext>(),
                ["TickerQ"] = scope.ServiceProvider.GetRequiredService<TickerQSchedulerDbContext>()
            };

            foreach (var (name, db) in dbContexts)
            {
                await MigrateAsync(db, name, stoppingToken);
            }

            _logger.LogInformation("All migrations applied successfully for {ModuleCount} modules", dbContexts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration service failed: {Error}", ex.Message);
            throw;
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task MigrateAsync(DbContext dbContext, string name, CancellationToken ct)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            var pending = await dbContext.Database.GetPendingMigrationsAsync(ct);

            if (pending.Any())
            {
                _logger.LogInformation("Applying {Count} pending migrations for {Module}...",
                    pending.Count(), name);
                await dbContext.Database.MigrateAsync(ct);
                _logger.LogInformation("{Module} migrations applied.", name);
            }
            else
            {
                _logger.LogInformation("{Module} database is up to date.", name);
            }
        });
    }
}
