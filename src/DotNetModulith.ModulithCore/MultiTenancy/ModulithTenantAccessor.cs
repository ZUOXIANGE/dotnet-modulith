using Finbuckle.MultiTenant.Abstractions;

namespace DotNetModulith.ModulithCore.MultiTenancy;

/// <summary>
/// 基于 Finbuckle 当前上下文的租户访问器。
/// </summary>
internal sealed class ModulithTenantAccessor : IModulithTenantAccessor
{
    private readonly IMultiTenantContextAccessor _multiTenantContextAccessor;

    public ModulithTenantAccessor(IMultiTenantContextAccessor multiTenantContextAccessor)
    {
        _multiTenantContextAccessor = multiTenantContextAccessor;
    }

    public ModulithTenantInfo? CurrentTenant
        => _multiTenantContextAccessor.MultiTenantContext?.TenantInfo as ModulithTenantInfo;

    public string GetRequiredTenantIdentifier()
        => CurrentTenant?.Identifier
           ?? throw new InvalidOperationException("A tenant identifier is required for this operation.");
}
