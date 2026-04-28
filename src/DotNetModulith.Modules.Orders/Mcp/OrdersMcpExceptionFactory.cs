using DotNetModulith.Abstractions.Exceptions;
using DotNetModulith.Abstractions.Results;
using Microsoft.AspNetCore.Http;

namespace DotNetModulith.Modules.Orders.Mcp;

internal static class OrdersMcpExceptionFactory
{
    public static Exception CreateUnauthorized()
        => new BusinessException(
            "Authentication is required.",
            ApiCodes.Common.Unauthorized,
            StatusCodes.Status401Unauthorized);

    public static Exception CreateForbidden(string permissionCode)
        => new BusinessException(
            $"Missing required permission: {permissionCode}.",
            ApiCodes.Common.Forbidden,
            StatusCodes.Status403Forbidden,
            new { permission = permissionCode });
}