namespace DotNetModulith.Api.MultiTenancy;

/// <summary>
/// 写入分布式缓存的租户上下文快照。
/// </summary>
public sealed record TenantContextSnapshot(
    string TenantId,
    string TenantIdentifier,
    string? TenantName,
    DateTimeOffset CapturedAt);
