using DotNetModulith.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DotNetModulith.Modules.Orders.Mcp;

public sealed class OrdersMcpAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<OrdersMcpAuthorizationService> _logger;

    public OrdersMcpAuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService,
        ILogger<OrdersMcpAuthorizationService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public Task EnsureManagePermissionAsync(CancellationToken cancellationToken)
        => EnsurePermissionAsync(PermissionCodes.OrdersManage, cancellationToken);

    public Task EnsureViewPermissionAsync(CancellationToken cancellationToken)
        => EnsurePermissionAsync(PermissionCodes.OrdersView, cancellationToken);

    private async Task EnsurePermissionAsync(string permissionCode, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw OrdersMcpExceptionFactory.CreateUnauthorized();

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            throw OrdersMcpExceptionFactory.CreateUnauthorized();
        }

        var result = await _authorizationService.AuthorizeAsync(user, resource: null, policyName: permissionCode);
        if (!result.Succeeded)
        {
            _logger.LogWarning("MCP permission denied: {PermissionCode}", permissionCode);
            throw OrdersMcpExceptionFactory.CreateForbidden(permissionCode);
        }
    }
}