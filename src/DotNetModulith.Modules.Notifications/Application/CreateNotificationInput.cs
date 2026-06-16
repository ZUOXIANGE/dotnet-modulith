namespace DotNetModulith.Modules.Notifications.Application;

public sealed record CreateNotificationInput(
    string Title,
    string Content,
    string Type,
    string RecipientId);
