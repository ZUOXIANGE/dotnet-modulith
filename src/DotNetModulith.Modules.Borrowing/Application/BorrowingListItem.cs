namespace DotNetModulith.Modules.Borrowing.Application;

public sealed record BorrowingListItem(
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
    DateTimeOffset CreatedAt);
