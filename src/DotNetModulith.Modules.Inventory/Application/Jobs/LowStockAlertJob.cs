using System.Diagnostics;
using System.Diagnostics.Metrics;
using DotNetCore.CAP;
using DotNetModulith.Abstractions.Contracts.Inventory;
using DotNetModulith.Modules.Inventory.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickerQ.Utilities.Base;

namespace DotNetModulith.Modules.Inventory.Application.Jobs;

/// <summary>
/// 周期性扫描低库存并发布预警事件
/// </summary>
public sealed class LowStockAlertJob
{
    private static readonly ActivitySource ActivitySource = new("DotNetModulith.Modules.Inventory");
    private static readonly Meter Meter = new("DotNetModulith.Modules.Inventory", "1.0.0");
    private static readonly Counter<long> ScanCount = Meter.CreateCounter<long>(
        "modulith.inventory.low_stock.scan_count",
        unit: "{scan}",
        description: "Number of low stock scans executed");
    private static readonly Counter<long> AlertCount = Meter.CreateCounter<long>(
        "modulith.inventory.low_stock.alert_count",
        unit: "{alert}",
        description: "Number of low stock items published as alerts");

    private readonly Domain.IStockRepository _stockRepository;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;
    private readonly IOptions<LowStockAlertOptions> _options;
    private readonly ILogger<LowStockAlertJob> _logger;

    public LowStockAlertJob(
        Domain.IStockRepository stockRepository,
        InventoryDbContext dbContext,
        ICapPublisher capPublisher,
        IOptions<LowStockAlertOptions> options,
        ILogger<LowStockAlertJob> logger)
    {
        _stockRepository = stockRepository;
        _dbContext = dbContext;
        _capPublisher = capPublisher;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// 每 5 分钟执行一次低库存扫描
    /// </summary>
    [TickerFunction("Inventory.LowStockAlertScan", cronExpression: "*/5 * * * *")]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        var options = _options.Value;

        using var activity = ActivitySource.StartActivity("Inventory.LowStockAlertScan", ActivityKind.Internal);
        activity?.SetTag("modulith.job_name", "Inventory.LowStockAlertScan");
        activity?.SetTag("modulith.threshold", options.Threshold);
        activity?.SetTag("modulith.batch_size", options.BatchSize);
        activity?.SetTag("modulith.ticker_id", context.Id);

        var scanTags = new TagList
        {
            { "modulith.job_name", "inventory_low_stock_scan" }
        };
        ScanCount.Add(1, scanTags);

        try
        {
            var lowStocks = await _stockRepository.GetLowStockAsync(
                options.Threshold,
                options.BatchSize,
                cancellationToken);

            var alertCandidates = lowStocks
                .Where(stock => stock.ShouldSendLowStockAlert(options.Threshold))
                .ToList();

            activity?.SetTag("modulith.low_stock_matches", lowStocks.Count);
            activity?.SetTag("modulith.low_stock_alert_candidates", alertCandidates.Count);

            if (alertCandidates.Count == 0)
            {
                _logger.LogDebug(
                    "Low stock scan completed with no new alerts. Threshold: {Threshold}, BatchSize: {BatchSize}",
                    options.Threshold,
                    options.BatchSize);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return;
            }

            var detectedAt = DateTimeOffset.UtcNow;
            var items = alertCandidates
                .Select(stock => new LowStockAlertItem(
                    stock.ProductId,
                    stock.ProductName,
                    stock.AvailableQuantity,
                    stock.ReservedQuantity))
                .ToArray();

            await CapTransactionScope.ExecuteAsync(
                _dbContext,
                _capPublisher,
                async ct =>
                {
                    foreach (var stock in alertCandidates)
                    {
                        stock.MarkLowStockAlertSent();
                        await _stockRepository.UpdateAsync(stock, ct);
                    }

                    await _capPublisher.PublishAsync(
                        "modulith.inventory.LowStockDetectedIntegrationEvent",
                        new LowStockDetectedIntegrationEvent(options.Threshold, detectedAt, items),
                        cancellationToken: ct);
                },
                cancellationToken);

            var alertTags = new TagList
            {
                { "modulith.job_name", "inventory_low_stock_scan" },
                { "modulith.threshold", options.Threshold }
            };
            AlertCount.Add(items.Length, alertTags);

            _logger.LogWarning(
                "Low stock alert published for {Count} items. Threshold: {Threshold}",
                items.Length,
                options.Threshold);

            activity?.SetTag("modulith.low_stock_published", items.Length);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("otel.status_code", "error");
            activity?.SetTag("otel.status_description", ex.Message);
            activity?.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    ["exception.type"] = ex.GetType().FullName,
                    ["exception.message"] = ex.Message,
                    ["exception.stacktrace"] = ex.ToString()
                }));
            _logger.LogError(ex, "Low stock scan failed");
            throw;
        }
    }
}
