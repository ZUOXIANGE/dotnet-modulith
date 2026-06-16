using DotNetModulith.Modules.Notifications.Api.Contracts.Responses;
using DotNetModulith.Modules.Notifications.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Notifications.Api.Mappings;

[Mapper]
public static partial class NotificationResponseMapper
{
    public static partial NotificationListItemResponse ToResponse(this NotificationListItem source);

    public static partial NotificationDetailsResponse ToResponse(this NotificationDetails source);
}
