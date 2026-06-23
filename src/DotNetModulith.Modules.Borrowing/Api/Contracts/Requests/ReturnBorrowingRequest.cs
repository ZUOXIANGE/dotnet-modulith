namespace DotNetModulith.Modules.Borrowing.Api.Contracts.Requests;

public sealed record ReturnBorrowingRequest
{
    public string? Notes { get; init; }
}
