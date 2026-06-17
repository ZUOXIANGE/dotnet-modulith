using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace DotNetModulith.Modules.Reports.Application.Subscribers;

public sealed class BorrowingEventSubscriber : ICapSubscribe
{
    private readonly IFusionCache _fusionCache;
    private readonly ILogger<BorrowingEventSubscriber> _logger;

    public BorrowingEventSubscriber(IFusionCache fusionCache, ILogger<BorrowingEventSubscriber> logger)
    {
        _fusionCache = fusionCache;
        _logger = logger;
    }

    [CapSubscribe(nameof(BookBorrowedIntegrationEvent))]
    public async Task HandleBookBorrowedAsync(BookBorrowedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Invalidating report caches due to book borrowed: {BookTitle}",
            @event.BookTitle);

        await InvalidateReportCachesAsync(ct);
    }

    [CapSubscribe(nameof(BookReturnedIntegrationEvent))]
    public async Task HandleBookReturnedAsync(BookReturnedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Invalidating report caches due to book returned: {BookTitle}",
            @event.BookTitle);

        await InvalidateReportCachesAsync(ct);
    }

    private async Task InvalidateReportCachesAsync(CancellationToken ct)
    {
        await _fusionCache.RemoveAsync("reports:borrowing_statistics", token: ct);
        await _fusionCache.RemoveAsync("reports:overdue_count", token: ct);
    }
}
