namespace DotNetModulith.Modules.Notifications.Api.Contracts.Responses;

public sealed record NotificationListResponse(
    NotificationListItemResponse[] Items,
    int Total,
    int Page,
    int PageSize);
