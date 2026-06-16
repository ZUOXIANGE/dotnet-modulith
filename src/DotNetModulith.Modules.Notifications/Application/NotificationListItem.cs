namespace DotNetModulith.Modules.Notifications.Application;

public sealed record NotificationListItem(
    Guid Id,
    string Title,
    string Content,
    string Type,
    string RecipientId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
