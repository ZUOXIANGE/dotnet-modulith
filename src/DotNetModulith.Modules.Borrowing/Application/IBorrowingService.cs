using DotNetModulith.Abstractions.Results;
using DotNetModulith.Modules.Books.Application;
using DotNetModulith.Modules.Borrowing.Domain;
using DotNetModulith.Modules.Borrowing.Infrastructure;
using DotNetModulith.Modules.Members.Application;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.Modules.Borrowing.Application;

public interface IBorrowingService
{
    Task<BorrowingListItem[]> GetBorrowingsAsync(string? keyword, string? status, int page, int pageSize, CancellationToken ct);
    Task<int> GetBorrowingsCountAsync(string? keyword, string? status, CancellationToken ct);
    Task<BorrowingDetails?> GetBorrowingByIdAsync(Guid id, CancellationToken ct);
    Task<BorrowingDetails> BorrowBookAsync(CreateBorrowingInput input, CancellationToken ct);
    Task<BorrowingDetails> ReturnBookAsync(Guid borrowingId, ReturnBorrowingInput input, CancellationToken ct);
    Task<BorrowingDetails> RenewBorrowingAsync(Guid borrowingId, CancellationToken ct);
    Task<BorrowingDetails> MarkLostAsync(Guid borrowingId, CancellationToken ct);
}
