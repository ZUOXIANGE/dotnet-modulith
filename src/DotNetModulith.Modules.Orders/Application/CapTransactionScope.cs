using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Orders.Application;

/// <summary>
/// CAP 事务作用域辅助类，确保数据库操作与 CAP 消息发布在同一事务中完成
/// </summary>
internal static class CapTransactionScope
{
    /// <summary>
    /// 在 CAP 事务作用域内执行操作
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="capPublisher">CAP 消息发布器</param>
    /// <param name="operation">要执行的操作</param>
    /// <param name="ct">取消令牌</param>
    public static Task ExecuteAsync(
        DbContext dbContext,
        ICapPublisher capPublisher,
        Func<CancellationToken, Task> operation,
        CancellationToken ct = default)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            if (capPublisher.ServiceProvider is null)
            {
                await using var fallbackTransaction = await dbContext.Database.BeginTransactionAsync(ct);
                await operation(ct);
                await dbContext.SaveChangesAsync(ct);
                await fallbackTransaction.CommitAsync(ct);
                return;
            }

            using var capTransaction = dbContext.Database.BeginTransaction(capPublisher, autoCommit: false);
            await operation(ct);
            await dbContext.SaveChangesAsync(ct);
            capTransaction.Commit();
        });
    }
}
