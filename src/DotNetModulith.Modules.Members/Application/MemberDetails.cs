namespace DotNetModulith.Modules.Members.Application;

public sealed record MemberDetails(
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
