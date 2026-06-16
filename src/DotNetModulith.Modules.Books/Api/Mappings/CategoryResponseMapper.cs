using DotNetModulith.Modules.Books.Api.Contracts.Responses;
using DotNetModulith.Modules.Books.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Books.Api.Mappings;

[Mapper]
public static partial class CategoryResponseMapper
{
    public static partial CategoryDetailsResponse ToResponse(this CategoryDetails source);
}
