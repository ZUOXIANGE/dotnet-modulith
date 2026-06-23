namespace DotNetModulith.Modules.Members.Api.Contracts.Responses;

public sealed record MemberListItemResponse(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    string MembershipType,
    string Status,
    int MaxBorrowCount,
    int CurrentBorrowCount,
    DateOnly JoinDate,
    DateOnly? ExpiryDate,
    DateTimeOffset CreatedAt);
