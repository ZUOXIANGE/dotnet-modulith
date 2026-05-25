using DotNetModulith.Abstractions.Results;
using DotNetModulith.ModulithCore.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Api.Controllers;

/// <summary>
/// 多租户上下文调试接口。
/// </summary>
[ApiController]
[Route("api/tenancy")]
[AllowAnonymous]
public sealed class TenancyController : ControllerBase
{
    private readonly IModulithTenantAccessor _tenantAccessor;

    public TenancyController(IModulithTenantAccessor tenantAccessor)
    {
        _tenantAccessor = tenantAccessor;
    }

    [HttpGet("current")]
    public ApiResponse<object> GetCurrentTenant()
    {
        var tenant = _tenantAccessor.CurrentTenant;
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Identifier))
        {
            return ApiResponse.Success<object>(new
            {
                resolved = false
            });
        }

        return ApiResponse.Success<object>(new
        {
            resolved = true,
            tenant.Id,
            tenant.Identifier,
            tenant.Name
        });
    }
}
