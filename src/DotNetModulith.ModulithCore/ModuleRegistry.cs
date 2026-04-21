using System.Collections.Concurrent;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块注册表，管理所有已注册模块的元数据和依赖关系
/// </summary>
public sealed class ModuleRegistry
{
    private readonly ConcurrentDictionary<string, ModuleDescriptor> _modules = new();
    private readonly List<ModuleDependencyEdge> _dependencyEdges = [];

    /// <summary>
    /// 所有已注册模块列表
    /// </summary>
    public IReadOnlyList<ModuleDescriptor> Modules => _modules.Values.ToList().AsReadOnly();

    /// <summary>
    /// 模块依赖边列表，表示模块间的依赖关系
    /// </summary>
    public IReadOnlyList<ModuleDependencyEdge> DependencyEdges => _dependencyEdges.AsReadOnly();

    public ModuleRegistry(IEnumerable<IModuleDescriptor> moduleDescriptors)
    {
        foreach (var descriptor in moduleDescriptors.Cast<ModuleDescriptor>())
        {
            if (!_modules.TryAdd(descriptor.Name, descriptor))
                throw new InvalidOperationException($"Module '{descriptor.Name}' is already registered.");

            foreach (var dep in descriptor.Dependencies)
            {
                _dependencyEdges.Add(new ModuleDependencyEdge(descriptor.Name, dep));
            }
        }
    }

    /// <summary>
    /// 根据模块名称获取模块描述符
    /// </summary>
    /// <param name="name">模块名称</param>
    /// <returns>模块描述符</returns>
    /// <exception cref="KeyNotFoundException">模块未注册时抛出</exception>
    public ModuleDescriptor GetModule(string name) =>
        _modules.TryGetValue(name, out var descriptor)
            ? descriptor
            : throw new KeyNotFoundException($"Module '{name}' is not registered.");

    /// <summary>
    /// 获取模块的拓扑排序结果，确保依赖模块先于依赖方初始化
    /// </summary>
    /// <returns>按拓扑排序的模块列表</returns>
    /// <exception cref="InvalidOperationException">存在循环依赖时抛出</exception>
    public IReadOnlyList<ModuleDescriptor> GetTopologicalOrder()
    {
        var inDegree = _modules.Keys.ToDictionary(k => k, _ => 0);
        var adjacency = _modules.Keys.ToDictionary(k => k, _ => new List<string>());

        foreach (var edge in _dependencyEdges)
        {
            adjacency[edge.From].Add(edge.To);
            inDegree[edge.To]++;
        }

        var queue = new Queue<string>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var result = new List<ModuleDescriptor>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(_modules[current]);

            foreach (var neighbor in adjacency[current])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        if (result.Count != _modules.Count)
            throw new InvalidOperationException("Circular dependency detected among modules.");

        result.Reverse();
        return result.AsReadOnly();
    }

    /// <summary>
    /// 检查模块间是否存在循环依赖
    /// </summary>
    /// <returns>存在循环依赖返回true，否则返回false</returns>
    public bool HasCircularDependency()
    {
        try
        {
            GetTopologicalOrder();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    /// <summary>
    /// 构建模块依赖关系图
    /// </summary>
    /// <returns>模块依赖关系图</returns>
    public ModuleDependencyGraph BuildDependencyGraph() =>
        new(_modules.Values.ToList().AsReadOnly(), _dependencyEdges.AsReadOnly());
}
