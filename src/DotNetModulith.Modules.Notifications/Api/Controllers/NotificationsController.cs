using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Notifications.Api.Contracts.Requests;
using DotNetModulith.Modules.Notifications.Api.Contracts.Responses;
using DotNetModulith.Modules.Notifications.Api.Mappings;
using DotNetModulith.Modules.Notifications.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Modules.Notifications.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [Authorize(Policy = NotificationPermissions.NotificationsView)]
    [HttpGet]
    public async Task<ApiResponse<NotificationListResponse>> GetNotifications(
        [FromQuery] string? recipientId,
        [FromQuery] bool? isRead,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var items = await _notificationService.GetNotificationsAsync(recipientId, isRead, page, pageSize, ct);
        var total = await _notificationService.GetNotificationsCountAsync(recipientId, isRead, ct);

        return ApiResponse.Success(new NotificationListResponse(
            items.Select(x => x.ToResponse()).ToArray(),
            total,
            page,
            pageSize));
    }

    [Authorize(Policy = NotificationPermissions.NotificationsView)]
    [HttpGet("unread-count")]
    public async Task<ApiResponse<UnreadCountResponse>> GetUnreadCount([FromQuery] string? recipientId, CancellationToken ct)
    {
        var count = await _notificationService.GetUnreadCountAsync(recipientId, ct);
        return ApiResponse.Success(new UnreadCountResponse(count));
    }

    [Authorize(Policy = NotificationPermissions.NotificationsView)]
    [HttpGet("{notificationId:guid}")]
    public async Task<ApiResponse<NotificationDetailsResponse>> GetNotification(Guid notificationId, CancellationToken ct)
    {
        var notification = await _notificationService.GetNotificationByIdAsync(notificationId, ct);
        if (notification is null)
            return ApiResponse.Failure<NotificationDetailsResponse>("notification not found", ApiCodes.Common.NotFound);

        return ApiResponse.Success(notification.ToResponse());
    }

    [Authorize(Policy = NotificationPermissions.NotificationsCreate)]
    [HttpPost]
    public async Task<ApiResponse<NotificationDetailsResponse>> CreateNotification([FromBody] CreateNotificationRequest request, CancellationToken ct)
    {
        var input = new CreateNotificationInput(request.Title, request.Content, request.Type, request.RecipientId);
        var result = await _notificationService.CreateNotificationAsync(input, ct);
        return ApiResponse.Success(result.ToResponse());
    }

    [Authorize(Policy = NotificationPermissions.NotificationsView)]
    [HttpPost("{notificationId:guid}/read")]
    public async Task<ApiResponse<object?>> MarkAsRead(Guid notificationId, CancellationToken ct)
    {
        await _notificationService.MarkAsReadAsync(notificationId, ct);
        return ApiResponse.Success();
    }

    [Authorize(Policy = NotificationPermissions.NotificationsView)]
    [HttpPost("read-all")]
    public async Task<ApiResponse<object?>> MarkAllAsRead([FromQuery] string? recipientId, CancellationToken ct)
    {
        await _notificationService.MarkAllAsReadAsync(recipientId, ct);
        return ApiResponse.Success();
    }
}
