namespace DotNetModulith.ModulithCore;

/// <summary>
/// 领域事件派发行为配置
/// </summary>
public sealed class DomainEventDispatcherOptions
{
    public const string SectionName = "DomainEventDispatcher";

    /// <summary>
    /// 为 true 时，单个处理器失败后记录日志并继续后续处理器
    /// 为 false 时，默认快速失败并向上抛出异常
    /// </summary>
    public bool ContinueOnHandlerError { get; set; }
}
