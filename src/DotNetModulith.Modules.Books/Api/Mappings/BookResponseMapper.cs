using DotNetModulith.Modules.Books.Api.Contracts.Responses;
using DotNetModulith.Modules.Books.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Books.Api.Mappings;

[Mapper]
public static partial class BookResponseMapper
{
    public static partial BookListItemResponse ToResponse(this BookListItem source);

    public static partial BookDetailsResponse ToResponse(this BookDetails source);
}
