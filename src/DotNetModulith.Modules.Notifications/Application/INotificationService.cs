namespace DotNetModulith.Modules.Notifications.Application;

public interface INotificationService
{
    Task<NotificationListItem[]> GetNotificationsAsync(string? recipientId, bool? isRead, int page, int pageSize, CancellationToken ct);
    Task<int> GetNotificationsCountAsync(string? recipientId, bool? isRead, CancellationToken ct);
    Task<int> GetUnreadCountAsync(string recipientId, CancellationToken ct);
    Task<NotificationDetails?> GetNotificationByIdAsync(Guid id, CancellationToken ct);
    Task<NotificationDetails> CreateNotificationAsync(CreateNotificationInput input, CancellationToken ct);
    Task MarkAsReadAsync(Guid notificationId, CancellationToken ct);
    Task MarkAllAsReadAsync(string recipientId, CancellationToken ct);
}
