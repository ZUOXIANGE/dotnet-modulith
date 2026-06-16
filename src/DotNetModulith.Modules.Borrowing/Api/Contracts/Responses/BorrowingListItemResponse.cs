namespace DotNetModulith.Modules.Borrowing.Api.Contracts.Responses;

public sealed record BorrowingListItemResponse(
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
