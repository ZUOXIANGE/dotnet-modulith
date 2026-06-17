using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Fines.Domain;
using DotNetModulith.Modules.Fines.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Modules.Fines.Application.Subscribers;

public sealed class OverdueFineSubscriber : ICapSubscribe
{
    private readonly FinesDbContext _dbContext;
    private readonly ILogger<OverdueFineSubscriber> _logger;
    private readonly OverdueFineOptions _options;

    public OverdueFineSubscriber(
        FinesDbContext dbContext,
        IOptions<OverdueFineOptions> options,
        ILogger<OverdueFineSubscriber> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
    }

    [CapSubscribe(nameof(BookOverdueIntegrationEvent))]
    public async Task HandleBookOverdueAsync(BookOverdueIntegrationEvent @event, CancellationToken ct)
    {
        var amount = @event.OverdueDays * _options.FinePerDay;

        if (amount <= 0)
            return;

        var entity = FineEntity.Create(
            @event.MemberId,
            @event.BorrowingRecordId,
            amount,
            FineReason.Overdue,
            DateTimeOffset.UtcNow);

        _dbContext.Fines.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Overdue fine created: {Amount:C} for {MemberName}, {OverdueDays} days overdue",
            amount,
            @event.MemberName,
            @event.OverdueDays);
    }
}
