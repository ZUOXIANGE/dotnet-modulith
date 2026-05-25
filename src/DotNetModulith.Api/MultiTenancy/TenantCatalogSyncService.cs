using DotNetModulith.ModulithCore.MultiTenancy;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Options;

namespace DotNetModulith.Api.MultiTenancy;

/// <summary>
/// 启动时将配置中的租户定义同步到 Finbuckle 分布式租户存储。
/// </summary>
public sealed class TenantCatalogSyncService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<ModulithMultiTenancyOptions> _options;

    public TenantCatalogSyncService(
        IServiceProvider serviceProvider,
        IOptions<ModulithMultiTenancyOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<ModulithTenantInfo>>();

        foreach (var tenant in _options.Value.Tenants)
        {
            if (string.IsNullOrWhiteSpace(tenant.Identifier))
            {
                continue;
            }

            var existing = await tenantStore.GetByIdentifierAsync(tenant.Identifier);
            if (existing is null)
            {
                await tenantStore.AddAsync(tenant);
                continue;
            }

            await tenantStore.UpdateAsync(tenant);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
