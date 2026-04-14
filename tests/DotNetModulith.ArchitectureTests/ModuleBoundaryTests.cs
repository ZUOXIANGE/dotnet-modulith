using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FluentAssertions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DotNetModulith.ArchitectureTests;

/// <summary>
/// 模块边界架构测试，验证各模块间不存在违规的直接依赖
/// </summary>
public class ModuleBoundaryTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Modules.Orders.Domain.Order).Assembly,
            typeof(Modules.Inventory.Domain.Stock).Assembly,
            typeof(Modules.Payments.Domain.Payment).Assembly,
            typeof(Modules.Notifications.Application.Subscribers.NotificationEventSubscriber).Assembly)
        .Build();

    private readonly IObjectProvider<IType> _ordersModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Orders*").As("Orders Module");

    private readonly IObjectProvider<IType> _inventoryModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Inventory*").As("Inventory Module");

    private readonly IObjectProvider<IType> _paymentsModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Payments*").As("Payments Module");

    private readonly IObjectProvider<IType> _notificationsModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Notifications*").As("Notifications Module");

    /// <summary>
    /// 验证订单模块不直接引用库存模块的领域层
    /// </summary>
    [Fact]
    public void OrdersModule_ShouldNotReferenceInventoryDomain()
    {
        var rule = Types().That().Are(_ordersModule)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.Inventory.Domain*"))
            .Because("Modules should communicate via integration events, not direct domain references")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    /// <summary>
    /// 验证订单模块不直接引用支付模块的领域层
    /// </summary>
    [Fact]
    public void OrdersModule_ShouldNotReferencePaymentsDomain()
    {
        var rule = Types().That().Are(_ordersModule)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.Payments.Domain*"))
            .Because("Modules should communicate via integration events, not direct domain references")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    /// <summary>
    /// 验证库存模块不直接引用订单模块的领域层
    /// </summary>
    [Fact]
    public void InventoryModule_ShouldNotReferenceOrdersDomain()
    {
        var rule = Types().That().Are(_inventoryModule)
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.Orders.Domain*"))
            .Because("Modules should communicate via integration events, not direct domain references")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    /// <summary>
    /// 验证领域层不依赖应用层（DDD依赖规则）
    /// </summary>
    [Fact]
    public void DomainLayer_ShouldNotReferenceApplicationLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Domain*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Application*"))
            .Because("Domain layer should not depend on application layer (DDD dependency rule)")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    /// <summary>
    /// 验证领域层不依赖基础设施层（DDD依赖规则）
    /// </summary>
    [Fact]
    public void DomainLayer_ShouldNotReferenceInfrastructureLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Domain*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Infrastructure*"))
            .Because("Domain layer should not depend on infrastructure layer (DDD dependency rule)")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }
}

/// <summary>
/// 模块注册表单元测试，验证循环依赖检测和拓扑排序
/// </summary>
public class ModuleRegistryTests
{
    private static ModulithCore.ModuleRegistry CreateRegistry(params ModulithCore.ModuleDescriptor[] descriptors)
        => new(descriptors);

    /// <summary>
    /// 验证模块注册表能正确检测循环依赖
    /// </summary>
    [Fact]
    public void ModuleRegistry_ShouldDetectCircularDependencies()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("A", "Ns.A", typeof(object).Assembly, ["B"]),
            new ModulithCore.ModuleDescriptor("B", "Ns.B", typeof(object).Assembly, ["C"]),
            new ModulithCore.ModuleDescriptor("C", "Ns.C", typeof(object).Assembly, ["A"]));

        registry.HasCircularDependency().Should().BeTrue();
    }

    /// <summary>
    /// 验证模块注册表能生成正确的拓扑排序
    /// </summary>
    [Fact]
    public void ModuleRegistry_ShouldProduceTopologicalOrder()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("Orders", "Ns.Orders", typeof(object).Assembly, ["Inventory"]),
            new ModulithCore.ModuleDescriptor("Inventory", "Ns.Inventory", typeof(object).Assembly, []),
            new ModulithCore.ModuleDescriptor("Payments", "Ns.Payments", typeof(object).Assembly, ["Orders"]),
            new ModulithCore.ModuleDescriptor("Notifications", "Ns.Notifications", typeof(object).Assembly, ["Orders", "Payments"]));

        var order = registry.GetTopologicalOrder();

        order.Select(m => m.Name).Should().ContainInConsecutiveOrder("Inventory", "Orders");
        order.Select(m => m.Name).Should().ContainInConsecutiveOrder("Orders", "Payments");
    }

    /// <summary>
    /// 验证依赖关系图能生成Mermaid格式输出
    /// </summary>
    [Fact]
    public void ModuleDependencyGraph_ShouldGenerateMermaid()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("Orders", "Ns.Orders", typeof(object).Assembly, ["Inventory"]),
            new ModulithCore.ModuleDescriptor("Inventory", "Ns.Inventory", typeof(object).Assembly, []));

        var graph = registry.BuildDependencyGraph();
        var mermaid = graph.ToMermaid();

        mermaid.Should().Contain("Orders --> Inventory");
    }
}
