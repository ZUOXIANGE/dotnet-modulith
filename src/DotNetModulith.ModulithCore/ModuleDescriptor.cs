using System.Reflection;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块描述符接口，提供模块的元数据信息
/// </summary>
public interface IModuleDescriptor
{
    /// <summary>
    /// 模块名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    string BaseNamespace { get; }

    /// <summary>
    /// 模块所属程序集
    /// </summary>
    Assembly Assembly { get; }

    /// <summary>
    /// 模块依赖列表
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    IReadOnlyList<string> PublishedEvents { get; }

    /// <summary>
    /// 模块订阅的集成事件列表
    /// </summary>
    IReadOnlyList<string> SubscribedEvents { get; }
}

/// <summary>
/// 模块描述符实现，封装模块的完整元数据
/// </summary>
public sealed class ModuleDescriptor : IModuleDescriptor
{
    /// <summary>
    /// 模块名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 模块基础命名空间
    /// </summary>
    public string BaseNamespace { get; }

    /// <summary>
    /// 模块所属程序集
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// 模块依赖列表
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// 模块命名接口列表
    /// </summary>
    public IReadOnlyList<string> NamedInterfaces { get; }

    /// <summary>
    /// 模块发布的集成事件列表
    /// </summary>
    public IReadOnlyList<string> PublishedEvents { get; }

    /// <summary>
    /// 模块订阅的集成事件列表
    /// </summary>
    public IReadOnlyList<string> SubscribedEvents { get; }

    public ModuleDescriptor(
        string name,
        string baseNamespace,
        Assembly assembly,
        IReadOnlyList<string>? dependencies = null,
        IReadOnlyList<string>? namedInterfaces = null,
        IReadOnlyList<string>? publishedEvents = null,
        IReadOnlyList<string>? subscribedEvents = null)
    {
        Name = name;
        BaseNamespace = baseNamespace;
        Assembly = assembly;
        Dependencies = dependencies ?? [];
        NamedInterfaces = namedInterfaces ?? [];
        PublishedEvents = publishedEvents ?? [];
        SubscribedEvents = subscribedEvents ?? [];
    }

    /// <summary>
    /// 返回模块描述符的字符串表示
    /// </summary>
    public override string ToString() =>
        $"Module: {Name} | Namespace: {BaseNamespace} | Dependencies: [{string.Join(", ", Dependencies)}] | Events Published: [{string.Join(", ", PublishedEvents)}] | Events Subscribed: [{string.Join(", ", SubscribedEvents)}]";
}
