using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FluentAssertions;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace DotNetModulith.ArchitectureTests;

public class ModuleBoundaryTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Modules.Notifications.NotificationsModule).Assembly,
            typeof(Modules.Users.Domain.UserEntity).Assembly)
        .Build();

    private readonly IObjectProvider<IType> _notificationsModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Notifications*").As("Notifications Module");

    private readonly IObjectProvider<IType> _usersModule =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Users*").As("Users Module");

    private readonly IObjectProvider<IType> _notificationsExternalModules =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Users*")
            .As("All non-Notifications modules");

    private readonly IObjectProvider<IType> _usersExternalModules =
        Types().That().ResideInNamespace("DotNetModulith.Modules.Notifications*")
            .As("All non-Users modules");

    [Fact]
    public void NotificationsModule_ShouldNotReferenceOtherModules()
    {
        var rule = Types().That().Are(_notificationsModule)
            .Should().NotDependOnAny(_notificationsExternalModules)
            .Because("Notifications should communicate with other modules only through abstractions and integration contracts")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void UsersModule_ShouldNotReferenceOtherModules()
    {
        var rule = Types().That().Are(_usersModule)
            .Should().NotDependOnAny(_usersExternalModules)
            .Because("Users should remain self-contained and expose auth capabilities without direct business module dependencies")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotReferenceApplicationLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Domain*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Application*"))
            .Because("Domain layer should not depend on application layer (DDD dependency rule)")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void DomainLayer_ShouldNotReferenceInfrastructureLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Domain*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Infrastructure*"))
            .Because("Domain layer should not depend on infrastructure layer (DDD dependency rule)")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void ApplicationLayer_ShouldNotReferenceApiLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Application*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Api*"))
            .Because("Application layer should remain transport-agnostic and must not depend on API contracts")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }

    [Fact]
    public void InfrastructureLayer_ShouldNotReferenceApiLayer()
    {
        var rule = Types().That().ResideInNamespace("DotNetModulith.Modules.*.Infrastructure*")
            .Should().NotDependOnAny(Types().That().ResideInNamespace("DotNetModulith.Modules.*.Api*"))
            .Because("Infrastructure layer should not depend on transport-specific API contracts")
            .WithoutRequiringPositiveResults();

        rule.Check(Architecture);
    }
}

public class ModuleRegistryTests
{
    private static ModulithCore.ModuleRegistry CreateRegistry(params ModulithCore.ModuleDescriptor[] descriptors)
        => new(descriptors);

    [Fact]
    public void ModuleRegistry_ShouldDetectCircularDependencies()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("A", "Ns.A", typeof(object).Assembly, ["B"]),
            new ModulithCore.ModuleDescriptor("B", "Ns.B", typeof(object).Assembly, ["C"]),
            new ModulithCore.ModuleDescriptor("C", "Ns.C", typeof(object).Assembly, ["A"]));

        registry.HasCircularDependency().Should().BeTrue();
    }

    [Fact]
    public void ModuleRegistry_ShouldProduceTopologicalOrder()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("Books", "Ns.Books", typeof(object).Assembly, []),
            new ModulithCore.ModuleDescriptor("Members", "Ns.Members", typeof(object).Assembly, ["Books"]),
            new ModulithCore.ModuleDescriptor("Borrowing", "Ns.Borrowing", typeof(object).Assembly, ["Books", "Members"]),
            new ModulithCore.ModuleDescriptor("Fines", "Ns.Fines", typeof(object).Assembly, ["Borrowing"]));

        var order = registry.GetTopologicalOrder();

        order.Select(m => m.Name).Should().ContainInConsecutiveOrder("Books", "Members");
        order.Select(m => m.Name).Should().ContainInConsecutiveOrder("Members", "Borrowing");
        order.Select(m => m.Name).Should().ContainInConsecutiveOrder("Borrowing", "Fines");
    }

    [Fact]
    public void ModuleDependencyGraph_ShouldGenerateMermaid()
    {
        var registry = CreateRegistry(
            new ModulithCore.ModuleDescriptor("Books", "Ns.Books", typeof(object).Assembly, []),
            new ModulithCore.ModuleDescriptor("Members", "Ns.Members", typeof(object).Assembly, ["Books"]));

        var graph = registry.BuildDependencyGraph();
        var mermaid = graph.ToMermaid();

        mermaid.Should().Contain("Members --> Books");
    }
}
