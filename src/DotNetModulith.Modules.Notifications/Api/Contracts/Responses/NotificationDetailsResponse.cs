namespace DotNetModulith.Modules.Notifications.Api.Contracts.Responses;

public sealed record NotificationDetailsResponse(
    Guid Id,
    string Title,
    string Content,
    string Type,
    string RecipientId,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
