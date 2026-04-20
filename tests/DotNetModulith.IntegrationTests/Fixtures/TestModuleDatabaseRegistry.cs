using DotNetModulith.Modules.Inventory.Infrastructure;
using DotNetModulith.Modules.Orders.Infrastructure;
using DotNetModulith.Modules.Payments.Infrastructure;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 集成测试使用的业务模块数据库注册中心
/// </summary>
internal static class TestModuleDatabaseRegistry
{
    public static IReadOnlyList<ModuleDatabaseRegistration> BusinessModules { get; } =
    [
        ModuleDatabaseRegistration.Create<OrdersDbContext>("orders"),
        ModuleDatabaseRegistration.Create<InventoryDbContext>("inventory"),
        ModuleDatabaseRegistration.Create<PaymentsDbContext>("payments"),
        ModuleDatabaseRegistration.Create<UsersDbContext>("users")
    ];
}

/// <summary>
/// 单个模块数据库的测试注册信息
/// </summary>
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
