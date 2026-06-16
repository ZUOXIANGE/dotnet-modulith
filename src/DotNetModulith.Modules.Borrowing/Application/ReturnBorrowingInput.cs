namespace DotNetModulith.Modules.Borrowing.Application;

public sealed record ReturnBorrowingInput(
    Guid BorrowingId,
    string? Notes = null);
