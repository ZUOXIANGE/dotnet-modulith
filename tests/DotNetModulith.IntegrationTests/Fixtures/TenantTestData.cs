using DotNetModulith.ModulithCore.MultiTenancy;
using DotNetModulith.Modules.Orders.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DotNetModulith.IntegrationTests.Fixtures;

internal static class TenantTestData
{
    public const string HeaderName = "X-Tenant-Id";
    public const string TenantA = "tenant-a";
    public const string TenantB = "tenant-b";

    public static ModulithTenantInfo CreateTenant(string identifier) => new()
    {
        Id = identifier,
        Identifier = identifier,
        Name = identifier
    };

    public static OrdersDbContext CreateOrdersDbContext(
        DbContextOptions<OrdersDbContext> options,
        string identifier = TenantA)
        => new(options, CreateTenant(identifier));
}
