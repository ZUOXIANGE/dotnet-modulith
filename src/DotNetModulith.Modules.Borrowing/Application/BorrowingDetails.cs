namespace DotNetModulith.Modules.Borrowing.Application;

public sealed record BorrowingDetails(
    Guid Id,
    Guid BookId,
    string BookTitle,
    Guid MemberId,
    string MemberName,
    DateOnly BorrowDate,
    DateOnly DueDate,
    DateOnly? ReturnDate,
    string Status,
    int RenewalCount,
    string Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
