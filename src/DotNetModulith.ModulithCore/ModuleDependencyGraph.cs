using System.Text;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块依赖关系图，支持导出为PlantUML和Mermaid格式
/// </summary>
public sealed class ModuleDependencyGraph
{
    /// <summary>
    /// 图中的模块列表
    /// </summary>
    public IReadOnlyList<ModuleDescriptor> Modules { get; }

    /// <summary>
    /// 图中的依赖边列表
    /// </summary>
    public IReadOnlyList<ModuleDependencyEdge> Edges { get; }

    public ModuleDependencyGraph(IReadOnlyList<ModuleDescriptor> modules, IReadOnlyList<ModuleDependencyEdge> edges)
    {
        Modules = modules;
        Edges = edges;
    }

    /// <summary>
    /// 导出为PlantUML格式
    /// </summary>
    /// <returns>PlantUML格式的依赖关系图</returns>
    public string ToPlantUml()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("skinparam componentStyle rectangle");

        foreach (var module in Modules)
        {
            sb.AppendLine($"component [{module.Name}] as {module.Name}");
        }

        foreach (var edge in Edges)
        {
            sb.AppendLine($"{edge.From} --> {edge.To}");
        }

        sb.AppendLine("@enduml");
        return sb.ToString();
    }

    /// <summary>
    /// 导出为Mermaid格式
    /// </summary>
    /// <returns>Mermaid格式的依赖关系图</returns>
    public string ToMermaid()
    {
        var sb = new StringBuilder();
        sb.AppendLine("graph TD");

        foreach (var edge in Edges)
        {
            sb.AppendLine($"    {edge.From} --> {edge.To}");
        }

        return sb.ToString();
    }
}
