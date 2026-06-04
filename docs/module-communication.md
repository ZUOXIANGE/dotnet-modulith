# 模块间通信规范（DotNetModulith）

借鉴 Spring Modulith 设计理念，模块间通信分为两种模式：

| 模式 | 适用场景 | 实现 |
|------|---------|------|
| 事件驱动（异步） | 最终一致性、跨模块解耦、长时间流程 | CAP + RabbitMQ Outbox |
| 同步调用（模块 API） | 实时校验、事务内操作、强一致性 | Spring Modulith API 模式 |

## 1. 事件驱动（异步）

### 1.1 事件定义

所有集成事件定义在 `DotNetModulith.Abstractions/Events`：

```csharp
namespace DotNetModulith.Abstractions.Events;

public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId, string CustomerId, decimal TotalAmount,
    List<OrderLineItem> Lines, DateTimeOffset CreatedAt);

public sealed record OrderLineItem(string ProductId, string ProductName,
    int Quantity, decimal UnitPrice);
```

### 1.2 事件声明

每个模块在 `IModule` 实现中声明发布和订阅的事件：

```csharp
// 库存模块声明：发布哪些事件、订阅哪些事件
public IReadOnlyList<string> PublishedEvents =>
[
    "modulith.inventory.StockReservedIntegrationEvent",
    "modulith.inventory.StockInsufficientIntegrationEvent",
];

public IReadOnlyList<string> SubscribedEvents =>
[
    "modulith.orders.OrderCreatedIntegrationEvent",
    "modulith.orders.OrderCancelledIntegrationEvent",
];
```

### 1.3 事件发布

使用 CAP 事务作用域，在数据库事务内发布事件（Outbox 模式）：

```csharp
await CapTransactionScope.ExecuteAsync(
    _dbContext,
    _capPublisher,
    async ct =>
    {
        await _orderRepository.AddAsync(order, ct);
        await _domainEventDispatcher.DispatchAsync([domainEvent], ct);
    },
    cancellationToken);
```

### 1.4 事件订阅

订阅者放在 `Application/Subscribers`：

```csharp
namespace DotNetModulith.Modules.Inventory.Application.Subscribers;

public sealed class OrderEventSubscriber : ICapSubscribe
{
    // [CapSubscribe(...)] 方法
}
```

## 2. 同步调用（Spring Modulith API 模式）

### 2.1 概念

受 Spring Modulith 启发，模块可以暴露公开 API 接口供其他模块直接调用。这适用于需要**同步响应**的场景（如实时库存校验、订单创建时的库存预留）。

对比 Spring Modulith：

| Spring Modulith | DotNetModulith |
|---|---|
| `@ApplicationModuleListener` 注解的事件方法 | CAP `[CapSubscribe]` 异步订阅 |
| `@NamedInterface("order")` 命名的模块内部调用 | DI 注入 `IXxxService` 接口 |
| `Architecture Tests` 强制执行模块边界 | ArchUnitNET 测试强制执行 |

### 2.2 实现步骤（以 Orders → Inventory 为例）

#### Step 1：被调用模块定义公开 API 接口

在 `Api` 命名空间下定义 `public interface` 和配套 DTO：

```csharp
// DotNetModulith.Modules.Inventory/Api/IInventoryService.cs
namespace DotNetModulith.Modules.Inventory.Api;

public interface IInventoryService
{
    Task<Result> CheckStockAsync(IReadOnlyList<CheckStockLine> lines, CancellationToken ct = default);
    Task<Result> ReserveStockAsync(string orderId, IReadOnlyList<ReserveStockLine> lines, CancellationToken ct = default);
}

public sealed record CheckStockLine(string ProductId, int Quantity);
public sealed record ReserveStockLine(string ProductId, int Quantity);
```

关键约束：
- 接口必须 `public`（对外公开）
- 接口和 DTO 都放在 `Api` 命名空间
- 返回类型使用 `DotNetModulith.Abstractions.Results.Result`
- 接口位置：[IInventoryService.cs](../src/DotNetModulith.Modules.Inventory/Api/IInventoryService.cs)

#### Step 2：实现类放在 Application 层

实现类标记为 `internal sealed`，外部模块无法直接实例化或引用：

```csharp
// DotNetModulith.Modules.Inventory/Application/Services/InventoryService.cs
namespace DotNetModulith.Modules.Inventory.Application.Services;

internal sealed class InventoryService : IInventoryService
{
    private readonly IStockRepository _stockRepository;
    private readonly InventoryDbContext _dbContext;
    private readonly ICapPublisher _capPublisher;

    // 构造函数注入
    public InventoryService(IStockRepository stockRepository,
        InventoryDbContext dbContext, ICapPublisher capPublisher) { ... }

    public async Task<Result> CheckStockAsync(...) { /* 实现 */ }
    public async Task<Result> ReserveStockAsync(...) { /* 实现 */ }
}
```

实现位置：[InventoryService.cs](../src/DotNetModulith.Modules.Inventory/Application/Services/InventoryService.cs)

#### Step 3：模块注册 DI

在 `IModule.AddModuleServices()` 中注册接口与实现：

```csharp
// DotNetModulith.Modules.Inventory/InventoryModule.cs
public IServiceCollection AddModuleServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddScoped<IInventoryService, InventoryService>();  // 关键行
    services.AddInventoryJobServices(configuration);
    services.AddTransient<OrderEventSubscriber>();
    return services;
}
```

注册位置：[InventoryModule.cs:L58](../src/DotNetModulith.Modules.Inventory/InventoryModule.cs#L58)

#### Step 4：调用模块添加项目引用

在调用模块的 `.csproj` 中添加对被调用模块的项目引用：

```xml
<!-- DotNetModulith.Modules.Orders.csproj -->
<ItemGroup>
    <ProjectReference Include="..\DotNetModulith.Modules.Inventory\DotNetModulith.Modules.Inventory.csproj" />
</ItemGroup>
```

#### Step 5：调用模块注入并使用

```csharp
// DotNetModulith.Modules.Orders/Application/Commands/CreateOrder/CreateOrderCommandHandler.cs
public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IInventoryService _inventoryService;

    public CreateOrderCommandHandler(..., IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async ValueTask<Guid> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        // 步骤 1：实时校验库存
        var checkResult = await _inventoryService.CheckStockAsync(
            command.Lines.Select(l => new CheckStockLine(l.ProductId, l.Quantity)).ToList(),
            cancellationToken);

        if (!checkResult.IsSuccess)
            throw new BusinessException(checkResult.Error!, ApiCodes.Inventory.InsufficientStock, 422);

        // ... 创建订单 ...

        // 步骤 2：同步预留库存
        var reserveResult = await _inventoryService.ReserveStockAsync(
            order.Id.ToString(),
            command.Lines.Select(l => new ReserveStockLine(l.ProductId, l.Quantity)).ToList(),
            cancellationToken);

        if (!reserveResult.IsSuccess)
            throw new BusinessException(reserveResult.Error!, ApiCodes.Inventory.InsufficientStock, 422);

        return order.Id;
    }
}
```

调用位置：[CreateOrderCommandHandler.cs](../src/DotNetModulith.Modules.Orders/Application/Commands/CreateOrder/CreateOrderCommandHandler.cs)

## 3. 架构测试强制执行

架构测试通过 ArchUnitNET 强制执行模块边界，确保模块只能通过公开 API 通信。

### 3.1 允许跨模块引用公开 API

Orders 模块**允许**引用 Inventory 模块的 `Api` 命名空间：

```csharp
// 这是合法的
using DotNetModulith.Modules.Inventory.Api;

public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    private readonly IInventoryService _inventoryService;
    // ...
}
```

### 3.2 禁止跨模块引用内部实现

Orders 模块**禁止**引用 Inventory 模块的内部层：

```csharp
// 以下引用会被架构测试拦截 ❌
using DotNetModulith.Modules.Inventory.Domain;         // 禁止！
using DotNetModulith.Modules.Inventory.Application;     // 禁止！
using DotNetModulith.Modules.Inventory.Infrastructure;  // 禁止！
```

### 3.3 架构测试代码

```csharp
// tests/DotNetModulith.ArchitectureTests/ModuleBoundaryTests.cs

// 外部模块 = 其他模块的内部层（排除公开 API 命名空间）
private readonly IObjectProvider<IType> _ordersExternalModules =
    Types().That().ResideInNamespace("DotNetModulith.Modules.Inventory.Application*")
        .Or().ResideInNamespace("DotNetModulith.Modules.Inventory.Domain*")
        .Or().ResideInNamespace("DotNetModulith.Modules.Inventory.Infrastructure*")
        .Or().ResideInNamespace("DotNetModulith.Modules.Payments*")
        .Or().ResideInNamespace("DotNetModulith.Modules.Notifications*")
        .Or().ResideInNamespace("DotNetModulith.Modules.Users*")
        .As("All non-Orders internal modules (excluding Inventory.Api)");

[Fact]
public void OrdersModule_ShouldNotReferenceOtherModuleInternals()
{
    var rule = Types().That().Are(_ordersModule)
        .Should().NotDependOnAny(_ordersExternalModules)
        .Because("Orders should communicate with other modules only through " +
                 "their published API (e.g., Inventory.Api) or integration events")
        .WithoutRequiringPositiveResults();

    rule.Check(Architecture);
}
```

架构测试位置：[ModuleBoundaryTests.cs](../tests/DotNetModulith.ArchitectureTests/ModuleBoundaryTests.cs)

### 3.4 完整规则清单

| 测试 | 检查内容 |
|------|---------|
| `OrdersModule_ShouldNotReferenceInventoryDomain` | Orders 不直接引用 Inventory.Domain |
| `OrdersModule_ShouldNotReferenceOtherModuleInternals` | Orders 不引用其他模块的 Application/Domain/Infrastructure |
| `DomainLayer_ShouldNotReferenceApplicationLayer` | Domain 不依赖 Application（DDD 规则） |
| `DomainLayer_ShouldNotReferenceInfrastructureLayer` | Domain 不依赖 Infrastructure（DDD 规则） |
| `ApplicationLayer_ShouldNotReferenceApiLayer` | Application 不依赖 Api（分层隔离） |

运行架构测试：

```bash
dotnet test tests/DotNetModulith.ArchitectureTests
```

## 4. 通信模式选择决策树

```
需要跨模块操作？
├── 可异步、解耦优先、最终一致性可接受
│   → 事件驱动（CAP + RabbitMQ Outbox）
│
└── 必须同步返回结果、属于同一请求内的事务
    └── 目标模块需要暴露公开 API？
        ├── 是 → 同步调用（Spring Modulith API 模式）
        └── 否 → 先让目标模块定义 `public interface IXxxService`
```

### 场景对照表

| 场景 | 适用模式 | 示例 |
|------|---------|------|
| 订单创建后发送通知 | 事件驱动 | `OrderCreatedIntegrationEvent` → Notification |
| 下单时实时校验库存 | 同步调用 | `IInventoryService.CheckStockAsync()` |
| 下单后扣减库存 | 同步调用 | `IInventoryService.ReserveStockAsync()` |
| 支付完成后更新订单状态 | 事件驱动 | `PaymentCompletedIntegrationEvent` → Order |
| 库存不足时通知管理员 | 事件驱动 | `StockInsufficientIntegrationEvent` → Notification |

## 5. 反模式与注意事项

### 5.1 不要绕过公开 API 直接访问内部层

```csharp
// 错误：直接注入 OrderDbContext 或 IOrderRepository ❌
public class InventoryService
{
    private readonly OrdersDbContext _ordersDb;  // 禁止！
}

// 正确：通过公开 API 接口调用 ✅
public class CreateOrderCommandHandler
{
    private readonly IInventoryService _inventoryService;  // 正确
}
```

### 5.2 不要将内部实现设为 public

```csharp
// 错误：公开了内部实现 ❌
public class InventoryService : IInventoryService { }

// 正确：实现类是 internal sealed ✅
internal sealed class InventoryService : IInventoryService { }
```

### 5.3 避免循环依赖

模块间同步调用必须是单向的。如果 A 调用 B，B 不应同步调用 A。遇到双向通信场景，至少一侧使用事件驱动。

### 5.4 不要滥用同步调用

同步调用会耦合模块的运行时生命周期。对于可以异步化的操作，优先使用事件驱动保持模块独立性。

## 6. 变更记录

| 日期 | 变更内容 |
|------|---------|
| 2026-05-28 | 初版：事件驱动 + Spring Modulith API 同步调用模式 |