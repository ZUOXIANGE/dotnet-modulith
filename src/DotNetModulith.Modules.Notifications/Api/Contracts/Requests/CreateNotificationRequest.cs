namespace DotNetModulith.Modules.Notifications.Api.Contracts.Requests;

public sealed record CreateNotificationRequest
{
    public required string Title { get; init; }

    public required string Content { get; init; }

    public required string Type { get; init; }

    public required string RecipientId { get; init; }
}
