namespace DotNetModulith.Modules.Users.Application;

/// <summary>
/// 用户模块种子初始化器
/// </summary>
public interface IUsersModuleSeeder
{
    Task SeedAsync(CancellationToken cancellationToken);
}
