using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetModulith.Modules.Users.Infrastructure;

/// <summary>
/// 用户模块数据库上下文设计时工厂
/// </summary>
internal sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<UsersDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=modulith;Username=postgres;Password=postgres",
            npgsql => npgsql.MigrationsAssembly(typeof(UsersDbContext).Assembly.FullName));

        return new UsersDbContext(optionsBuilder.Options);
    }
}
