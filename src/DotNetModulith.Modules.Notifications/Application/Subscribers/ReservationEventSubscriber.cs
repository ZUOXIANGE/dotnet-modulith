using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Notifications.Domain;
using DotNetModulith.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Notifications.Application.Subscribers;

public sealed class ReservationEventSubscriber : ICapSubscribe
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<ReservationEventSubscriber> _logger;

    public ReservationEventSubscriber(NotificationsDbContext dbContext, ILogger<ReservationEventSubscriber> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [CapSubscribe(nameof(ReservationAvailableIntegrationEvent))]
    public async Task HandleReservationAvailableAsync(ReservationAvailableIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Reservation created: {BookTitle} for {MemberName}, expires {ExpiryDate}",
            @event.BookTitle,
            @event.MemberName,
            @event.ExpiryDate);

        var notification = NotificationEntity.Create(
            "预约成功",
            $"您已成功预约《{@event.BookTitle}》，请在 {@event.ExpiryDate:yyyy-MM-dd} 前到馆借阅",
            NotificationType.ReservationAvailable,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }

    [CapSubscribe(nameof(ReservationExpiredIntegrationEvent))]
    public async Task HandleReservationExpiredAsync(ReservationExpiredIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Reservation expired: {BookTitle} for {MemberName}",
            @event.BookTitle,
            @event.MemberName);

        var notification = NotificationEntity.Create(
            "预约已过期",
            $"您对《{@event.BookTitle}》的预约已于 {@event.ExpiryDate:yyyy-MM-dd} 过期",
            NotificationType.Overdue,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }
}
