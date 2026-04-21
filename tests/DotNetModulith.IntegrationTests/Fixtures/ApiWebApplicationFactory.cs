using DotNetCore.CAP;
using DotNetModulith.Modules.Users;
using DotNetModulith.Modules.Users.Domain;
using DotNetModulith.Modules.Users.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// 自定义WebApplicationFactory，使用Testcontainers PostgreSQL替代真实数据库连接，
/// 并在测试环境中注册CAP的空实现以避免对RabbitMQ的依赖
/// </summary>
public sealed class ApiWebApplicationFactory : TestWebApplicationFactoryBase
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:modulithdb"] = ConnectionString,
                ["ConnectionStrings:redis"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            DbFixture.ReplaceRegisteredDbContexts(services);
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<ICapPublisher, NullCapPublisher>();
        });
    }

    public async Task InitializeUsersModuleAsync()
    {
        await DbFixture.ResetAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.SeedUsersModuleAsync();

        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserEntity>>();
        var adminUser = await dbContext.Users
            .AsTracking()
            .SingleAsync(x => x.UserName == "admin");

        var now = DateTimeOffset.UtcNow;
        adminUser.SetPassword(passwordHasher.HashPassword(adminUser, "Admin@123456"), now);
        if (!adminUser.IsActive)
        {
            adminUser.SetActive(true, now);
        }

        await dbContext.SaveChangesAsync();
    }
}
