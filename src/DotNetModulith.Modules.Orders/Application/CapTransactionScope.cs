using DotNetCore.CAP;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Orders.Application;

internal static class CapTransactionScope
{
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
