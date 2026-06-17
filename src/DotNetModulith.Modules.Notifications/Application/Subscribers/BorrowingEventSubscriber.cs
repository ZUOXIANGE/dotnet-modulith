using DotNetCore.CAP;
using DotNetModulith.Abstractions.Events;
using DotNetModulith.Modules.Notifications.Domain;
using DotNetModulith.Modules.Notifications.Infrastructure;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Notifications.Application.Subscribers;

public sealed class BorrowingEventSubscriber : ICapSubscribe
{
    private readonly NotificationsDbContext _dbContext;
    private readonly ILogger<BorrowingEventSubscriber> _logger;

    public BorrowingEventSubscriber(NotificationsDbContext dbContext, ILogger<BorrowingEventSubscriber> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [CapSubscribe(nameof(BookBorrowedIntegrationEvent))]
    public async Task HandleBookBorrowedAsync(BookBorrowedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Book borrowed: {BookTitle} by {MemberName}, due {DueDate}",
            @event.BookTitle,
            @event.MemberName,
            @event.DueDate);

        var notification = NotificationEntity.Create(
            "借阅成功",
            $"您已成功借阅《{@event.BookTitle}》，应还日期为 {@event.DueDate:yyyy-MM-dd}",
            NotificationType.BorrowDue,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }

    [CapSubscribe(nameof(BookReturnedIntegrationEvent))]
    public async Task HandleBookReturnedAsync(BookReturnedIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogInformation(
            "Book returned: {BookTitle} by {MemberName}",
            @event.BookTitle,
            @event.MemberName);

        var notification = NotificationEntity.Create(
            "归还成功",
            $"您已成功归还《{@event.BookTitle}》，归还日期为 {@event.ReturnDate:yyyy-MM-dd}",
            NotificationType.BorrowDue,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }

    [CapSubscribe(nameof(BookOverdueIntegrationEvent))]
    public async Task HandleBookOverdueAsync(BookOverdueIntegrationEvent @event, CancellationToken ct)
    {
        _logger.LogWarning(
            "Book overdue: {BookTitle} by {MemberName}, {OverdueDays} days overdue",
            @event.BookTitle,
            @event.MemberName,
            @event.OverdueDays);

        var notification = NotificationEntity.Create(
            "图书逾期提醒",
            $"您借阅的《{@event.BookTitle}》已逾期 {@event.OverdueDays} 天，请尽快归还",
            NotificationType.Overdue,
            @event.MemberId.ToString(),
            DateTimeOffset.UtcNow);

        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);
    }
}
