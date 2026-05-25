using DotNetModulith.ModulithCore.MultiTenancy;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.AspNetCore.Extensions;
using Finbuckle.MultiTenant.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetModulith.Api.MultiTenancy;

internal static class MultiTenancyServiceCollectionExtensions
{
    public static IServiceCollection AddModulithMultiTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ModulithMultiTenancyOptions>()
            .Bind(configuration.GetSection(ModulithMultiTenancyOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => options.Tenants.All(t =>
                !string.IsNullOrWhiteSpace(t.Id) &&
                !string.IsNullOrWhiteSpace(t.Identifier)),
                "Each configured tenant must provide both Id and Identifier.")
            .ValidateOnStart();

        var redisConnection = configuration.GetConnectionString("redis");
        services.RemoveAll<IDistributedCache>();
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "dotnet-modulith:";
            });
        }

        var options = configuration
            .GetSection(ModulithMultiTenancyOptions.SectionName)
            .Get<ModulithMultiTenancyOptions>() ?? new ModulithMultiTenancyOptions();

        services.AddMultiTenant<ModulithTenantInfo>()
            .WithHeaderStrategy(options.HeaderName)
            .WithDistributedCacheStore(options.ContextCacheTtl);

        services.AddSingleton<DistributedTenantContextStore>();
        services.AddHostedService<TenantCatalogSyncService>();

        return services;
    }
}
