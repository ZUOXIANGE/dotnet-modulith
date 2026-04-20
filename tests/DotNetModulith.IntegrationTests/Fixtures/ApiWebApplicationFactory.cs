using DotNetCore.CAP;
using DotNetModulith.Modules.Users;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 自定义WebApplicationFactory，使用Testcontainers PostgreSQL替代真实数据库连接，
/// 并在测试环境中注册CAP的空实现以避免对RabbitMQ的依赖
/// </summary>
public sealed class ApiWebApplicationFactory : TestWebApplicationFactoryBase
{
    private bool _usersModuleInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:modulithdb"] = ConnectionString
            });
        });

        builder.ConfigureServices(services =>
        {
            DbFixture.ReplaceRegisteredDbContexts(services);
            services.AddSingleton<ICapPublisher, NullCapPublisher>();
        });
    }

    public async Task InitializeUsersModuleAsync()
    {
        if (_usersModuleInitialized)
        {
            return;
        }

        using var scope = Services.CreateScope();
        var usersDb = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await usersDb.Database.MigrateAsync();
        await scope.ServiceProvider.SeedUsersModuleAsync();
        _usersModuleInitialized = true;
    }
}
