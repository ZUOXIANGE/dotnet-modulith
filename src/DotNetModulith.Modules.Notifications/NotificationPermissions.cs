namespace DotNetModulith.Modules.Notifications;

public static class NotificationPermissions
{
    public const string NotificationsView = "notifications.view";
    public const string NotificationsManage = "notifications.manage";

    public static readonly IReadOnlyList<string> All = [NotificationsView, NotificationsManage];
}
