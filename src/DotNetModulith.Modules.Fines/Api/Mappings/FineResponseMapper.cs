using DotNetModulith.Modules.Fines.Api.Contracts.Responses;
using DotNetModulith.Modules.Fines.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Fines.Api.Mappings;

[Mapper]
public static partial class FineResponseMapper
{
    public static partial FineListItemResponse ToResponse(this FineListItem source);

    public static partial FineDetailsResponse ToResponse(this FineDetails source);
}
