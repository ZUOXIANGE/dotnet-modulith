namespace DotNetModulith.ModulithCore.MultiTenancy;

/// <summary>
/// 当前执行上下文的租户访问器。
/// </summary>
public interface IModulithTenantAccessor
{
    ModulithTenantInfo? CurrentTenant { get; }

    string GetRequiredTenantIdentifier();
}
