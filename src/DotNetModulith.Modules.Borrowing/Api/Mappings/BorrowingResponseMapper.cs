using DotNetModulith.Modules.Borrowing.Api.Contracts.Responses;
using DotNetModulith.Modules.Borrowing.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Borrowing.Api.Mappings;

[Mapper]
public static partial class BorrowingResponseMapper
{
    public static partial BorrowingListItemResponse ToResponse(this BorrowingListItem source);

    public static partial BorrowingDetailsResponse ToResponse(this BorrowingDetails source);
}
