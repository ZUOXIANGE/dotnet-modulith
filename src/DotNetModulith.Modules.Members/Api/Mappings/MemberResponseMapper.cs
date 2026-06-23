using DotNetModulith.Modules.Members.Api.Contracts.Responses;
using DotNetModulith.Modules.Members.Application;
using Riok.Mapperly.Abstractions;

namespace DotNetModulith.Modules.Members.Api.Mappings;

[Mapper]
public static partial class MemberResponseMapper
{
    public static partial MemberListItemResponse ToResponse(this MemberListItem source);

    public static partial MemberDetailsResponse ToResponse(this MemberDetails source);
}
