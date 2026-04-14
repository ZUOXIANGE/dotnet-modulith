using System.Reflection;

namespace DotNetModulith.ModulithCore;

/// <summary>
/// 模块边界验证器，检测模块间是否存在未声明的隐式依赖
/// </summary>
public sealed class ModuleBoundaryVerifier
{
    private readonly ModuleRegistry _registry;

    public ModuleBoundaryVerifier(ModuleRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// 验证所有模块的边界，检测未声明的隐式依赖
    /// </summary>
    /// <returns>边界违规列表</returns>
    public IReadOnlyList<BoundaryViolation> VerifyBoundaries()
    {
        var violations = new List<BoundaryViolation>();

        foreach (var module in _registry.Modules)
        {
            var moduleAssembly = module.Assembly;
            var referencedTypes = GetTypesReferencingOtherModules(module);

            foreach (var reference in referencedTypes)
            {
                if (!module.Dependencies.Contains(reference.TargetModule))
                {
                    violations.Add(new BoundaryViolation(
                        module.Name,
                        reference.TargetModule,
                        reference.SourceType,
                        reference.ReferencedType,
                        "Implicit dependency not declared in module descriptor"));
                }
            }
        }

        return violations.AsReadOnly();
    }

    private List<ModuleTypeReference> GetTypesReferencingOtherModules(ModuleDescriptor module)
    {
        var references = new List<ModuleTypeReference>();
        var otherModules = _registry.Modules.Where(m => m.Name != module.Name).ToList();
        var moduleAssembly = module.Assembly;

        foreach (var otherModule in otherModules)
        {
            var otherNamespace = otherModule.BaseNamespace;

            foreach (var type in moduleAssembly.GetTypes())
            {
                if (!type.Namespace?.StartsWith(module.BaseNamespace) == true)
                    continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    CheckTypeReferences(method.ReturnType, type, otherModule, references);

                    foreach (var param in method.GetParameters())
                    {
                        CheckTypeReferences(param.ParameterType, type, otherModule, references);
                    }
                }

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    CheckTypeReferences(field.FieldType, type, otherModule, references);
                }

                foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    CheckTypeReferences(prop.PropertyType, type, otherModule, references);
                }
            }
        }

        return references;
    }

    private static void CheckTypeReferences(
        Type referencedType,
        Type sourceType,
        ModuleDescriptor targetModule,
        List<ModuleTypeReference> references)
    {
        if (referencedType.Namespace?.StartsWith(targetModule.BaseNamespace) == true)
        {
            references.Add(new ModuleTypeReference(
                targetModule.Name,
                sourceType.FullName ?? sourceType.Name,
                referencedType.FullName ?? referencedType.Name));
        }
    }
}

/// <summary>
/// 边界违规记录，描述模块间未声明的隐式依赖
/// </summary>
/// <param name="SourceModule">违规的源模块名称</param>
/// <param name="TargetModule">被隐式引用的目标模块名称</param>
/// <param name="SourceType">源模块中引用目标模块的类型</param>
/// <param name="ReferencedType">被引用的目标模块类型</param>
/// <param name="Description">违规描述</param>
public sealed record BoundaryViolation(
    string SourceModule,
    string TargetModule,
    string SourceType,
    string ReferencedType,
    string Description);

/// <summary>
/// 模块类型引用记录，描述模块间类型的引用关系
/// </summary>
/// <param name="TargetModule">被引用的目标模块名称</param>
/// <param name="SourceType">发起引用的源类型全名</param>
/// <param name="ReferencedType">被引用的目标类型全名</param>
public sealed record ModuleTypeReference(
    string TargetModule,
    string SourceType,
    string ReferencedType);
