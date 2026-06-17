namespace DotNetModulith.Modules.Notifications;

public static class NotificationPermissions
{
    public const string NotificationsView = "notifications.view";
    public const string NotificationsCreate = "notifications.create";
    public const string NotificationsSend = "notifications.send";
    public const string NotificationsDelete = "notifications.delete";

    public static readonly IReadOnlyList<string> All =
    [
        NotificationsView, NotificationsCreate, NotificationsSend, NotificationsDelete
    ];
}
