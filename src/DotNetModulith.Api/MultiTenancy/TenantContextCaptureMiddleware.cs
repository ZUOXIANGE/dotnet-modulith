using DotNetModulith.ModulithCore.MultiTenancy;

namespace DotNetModulith.Api.MultiTenancy;

/// <summary>
/// 在请求完成前将解析出的租户上下文快照落入分布式缓存。
/// </summary>
public sealed class TenantContextCaptureMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextCaptureMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IModulithTenantAccessor tenantAccessor,
        DistributedTenantContextStore tenantContextStore)
    {
        var tenant = tenantAccessor.CurrentTenant;
        if (tenant?.Identifier is not null)
        {
            await tenantContextStore.StoreAsync(tenant, context.RequestAborted);
        }

        await _next(context);
    }
}
