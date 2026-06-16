namespace DotNetModulith.Modules.Members.Api.Contracts.Responses;

public sealed record MemberDetailsResponse(
    Guid Id,
    string Name,
    string Phone,
    string Email,
    string Address,
    string MembershipType,
    string Status,
    int MaxBorrowCount,
    int CurrentBorrowCount,
    DateOnly JoinDate,
    DateOnly? ExpiryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
