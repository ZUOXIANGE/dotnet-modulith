using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Notifications.Domain;
using DotNetModulith.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Notifications.Application.Subscribers;

public sealed class FineEventSubscriber : ICapSubscribe
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<FineEventSubscriber> _logger;

    public FineEventSubscriber(NotificationsDbContext dbContext, ILogger<FineEventSubscriber> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [CapSubscribe(nameof(FineCreatedIntegrationEvent))]
    public async Task HandleFineCreatedAsync(FineCreatedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogWarning(
            "Fine created: {Amount:C} for {MemberName}, reason: {Reason}",
            @event.Amount,
            @event.MemberName,
            @event.Reason);

        var notification = NotificationEntity.Create(
            "罚款通知",
            $"您有一笔罚款：{@event.Amount:C}，原因：{@event.Reason}，请及时缴纳",
            NotificationType.FineIssued,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }

    [CapSubscribe(nameof(FinePaidIntegrationEvent))]
    public async Task HandleFinePaidAsync(FinePaidIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Fine paid: {Amount:C} by {MemberName}",
            @event.Amount,
            @event.MemberName);

        var notification = NotificationEntity.Create(
            "罚款已缴纳",
            $"您已成功缴纳罚款 {@event.Amount:C}",
            NotificationType.System,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }
}
