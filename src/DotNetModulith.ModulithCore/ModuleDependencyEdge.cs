namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块依赖边，表示从源模块到目标模块的依赖关系
/// </summary>
/// <param name="From">依赖方模块名称</param>
/// <param name="To">被依赖模块名称</param>
public sealed record ModuleDependencyEdge(string From, string To);
