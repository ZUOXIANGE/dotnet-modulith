using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.IntegrationTests.Fixtures;

internal static class TestModuleDatabaseRegistry
{
    public static IReadOnlyList<ModuleDatabaseRegistration> BusinessModules { get; } =
    [
        ModuleDatabaseRegistration.Create<UsersDbContext>("users")
    ];
}

internal sealed record ModuleDatabaseRegistration(
    string Schema,
    Func<string, DbContext> CreateDbContext,
    Action<IServiceCollection, string> ReplaceDbContext)
{
    public static ModuleDatabaseRegistration Create<TDbContext>(string schema)
        where TDbContext : DbContext
        => new(
            schema,
            connectionString => (DbContext)Activator.CreateInstance(
                typeof(TDbContext),
                new DbContextOptionsBuilder<TDbContext>()
                    .UseNpgsql(connectionString)
                    .Options)!,
            (services, connectionString) =>
            {
                var descriptor = services.FirstOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TDbContext>));
                if (descriptor is not null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TDbContext>(options => options.UseNpgsql(connectionString));
            });
}
