namespace DotNetModulith.Modules.Notifications.Domain;

public sealed class NotificationEntity
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public NotificationType Type { get; private set; }
    public string RecipientId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private NotificationEntity()
    {
        Title = string.Empty;
        Content = string.Empty;
        RecipientId = string.Empty;
    }

    public static NotificationEntity Create(
        string title,
        string content,
        NotificationType type,
        string recipientId,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Type = type,
            RecipientId = recipientId,
            IsRead = false,
            CreatedAt = now,
            ReadAt = null
        };

    public void MarkAsRead(DateTimeOffset now)
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = now;
    }
}
