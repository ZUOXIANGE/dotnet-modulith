using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace DotNetModulith.IntegrationTests.Fixtures;

/// <summary>
/// PostgreSQL测试容器夹具，提供独立的PostgreSQL数据库实例用于集成测试
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("modulith_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    private Respawner? _respawner;

    /// <summary>
    /// 数据库连接字符串
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// 使用测试容器连接串替换已注册的业务模块 DbContext
    /// </summary>
    public void ReplaceRegisteredDbContexts(IServiceCollection services)
    {
        foreach (var module in TestModuleDatabaseRegistry.BusinessModules)
        {
            module.ReplaceDbContext(services, ConnectionString);
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await EnsureMigrationsAppliedAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public", .. TestModuleDatabaseRegistry.BusinessModules.Select(module => module.Schema)]
        });
    }

    /// <summary>
    /// 确保数据库迁移已应用，使表结构存在
    /// </summary>
    private async Task EnsureMigrationsAppliedAsync()
    {
        foreach (var module in TestModuleDatabaseRegistry.BusinessModules)
        {
            await using var dbContext = module.CreateDbContext(ConnectionString);
            await dbContext.Database.MigrateAsync();
        }
    }

    /// <summary>
    /// 重置数据库，清除所有测试数据
    /// </summary>
    public async Task ResetAsync()
    {
        if (_respawner is null)
            return;

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
