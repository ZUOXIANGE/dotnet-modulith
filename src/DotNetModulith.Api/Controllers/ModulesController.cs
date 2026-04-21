using DotNetModulith.Abstractions.Authorization;
using DotNetModulith.Abstractions.Results;
using DotNetModulith.ModulithCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetModulith.Api.Controllers;

/// <summary>
/// 模块治理相关接口
/// </summary>
[ApiController]
[Route("api/modules")]
public sealed class ModulesController : ControllerBase
{
    private readonly ModuleRegistry _registry;
    private readonly ModuleBoundaryVerifier _verifier;

    public ModulesController(ModuleRegistry registry, ModuleBoundaryVerifier verifier)
    {
        _registry = registry;
        _verifier = verifier;
    }

    /// <summary>
    /// 获取已注册模块列表
    /// </summary>
    /// <returns>模块列表信息。</returns>
    [Authorize(Policy = PermissionCodes.ModulesView)]
    [HttpGet]
    public ApiResponse<object> GetModules()
    {
        var modules = _registry.Modules.Select(m => new
        {
            m.Name,
            m.BaseNamespace,
            Dependencies = m.Dependencies,
            PublishedEvents = m.PublishedEvents,
            SubscribedEvents = m.SubscribedEvents
        });

        return ApiResponse.Success<object>(modules);
    }

    /// <summary>
    /// 获取模块依赖图
    /// </summary>
    /// <returns>包含 Mermaid 与 PlantUML 的依赖图文本。</returns>
    [Authorize(Policy = PermissionCodes.ModulesView)]
    [HttpGet("graph")]
    public ApiResponse<object> GetModuleGraph()
    {
        var graph = _registry.BuildDependencyGraph();
        return ApiResponse.Success<object>(new
        {
            Mermaid = graph.ToMermaid(),
            PlantUml = graph.ToPlantUml()
        });
    }

    /// <summary>
    /// 执行模块边界校验
    /// </summary>
    /// <returns>边界校验结果。</returns>
    [Authorize(Policy = PermissionCodes.ModulesView)]
    [HttpGet("verify")]
    public ApiResponse<object> VerifyModuleBoundaries()
    {
        var violations = _verifier.VerifyBoundaries();
        return violations.Count == 0
            ? ApiResponse.Success<object>(new { status = "healthy", violations = 0 })
            : ApiResponse.Success<object>(new { status = "violations_detected", violations });
    }
}
