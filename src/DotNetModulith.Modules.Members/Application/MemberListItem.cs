namespace DotNetModulith.Modules.Members.Application;

public sealed record MemberListItem(
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
