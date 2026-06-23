namespace DotNetModulith.Modules.Borrowing.Application;

public sealed record CreateBorrowingInput(
    Guid BookId,
    Guid MemberId,
    int BorrowDays = 30);
