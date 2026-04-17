# DotNetModulith

基于 ASP.NET Core 10 的模块化单体架构，借鉴 [Spring Modulith](https://spring.io/projects/spring-modulith) 的设计思想，实现领域驱动设计（DDD）、事件驱动架构和模块边界管理。

## 特性

- **模块化单体** — 每个业务模块独立定义边界、依赖关系和事件契约，支持未来拆分为微服务
- **领域驱动设计** — 聚合根、值对象、领域事件、仓储等核心 DDD 模式
- **事件驱动架构** — 基于 [DotNetCore.CAP](https://github.com/dotnetcore/CAP) 实现可靠的跨模块事件发布/订阅，支持 Outbox 模式
- **模块边界验证** — 编译时和运行时双重检测模块间的违规依赖
- **CQRS** — 基于 [Mediator](https://github.com/martinothamar/Mediator) 源生成器实现命令/查询分离
- **OpenTelemetry** — 完整的分布式追踪、指标和日志可观测性，通过 OTLP 导出到 [OpenObserve](https://openobserve.ai/)
- **API 文档** — 集成 [Scalar](https://github.com/scalar/scalar) 生成交互式 API 文档
- **.NET Aspire** — 开发环境编排，自动管理 PostgreSQL、RabbitMQ 和数据库迁移
- **弹性处理** — 集成 Microsoft.Extensions.Http.Resilience 提供 HTTP 重试和熔断

## 技术栈

| 类别     | 技术                                         |
| -------- | -------------------------------------------- |
| 运行时   | .NET 10                                      |
| Web 框架 | ASP.NET Core 10 Minimal API                  |
| 数据库   | PostgreSQL (Npgsql / EF Core 10)             |
| 消息代理 | RabbitMQ                                     |
| 事件总线 | DotNetCore.CAP 8.3                           |
| 缓存     | FusionCache (L1 Memory + L2 Redis)           |
| Mediator | Mediator 3.0 (源生成)                        |
| 对象映射 | Riok.Mapperly 4.3 (源生成)                   |
| 可观测性 | OpenTelemetry + OpenObserve + OTEL Collector |
| API 文档 | Scalar.AspNetCore 2.x                        |
| 编排     | .NET Aspire 9.2                              |
| 测试     | xUnit v3 + FluentAssertions + Testcontainers |
| 架构测试 | ArchUnitNET                                  |
| 代码规范 | Husky.Net + EditorConfig + dotnet format     |
| 包管理   | Central Package Management                   |

## 项目结构

```
DotNetModulith/
├── src/
│   ├── DotNetModulith.Abstractions/          # 核心抽象（聚合根、事件、Result 模式）
│   │   ├── Contracts/                        #   集成事件契约（跨模块共享）
│   │   ├── Domain/                           #   聚合根、实体接口
│   │   ├── Events/                           #   领域事件 & 集成事件基类
│   │   └── Results/                          #   Result 模式
│   │
│   ├── DotNetModulith.ModulithCore/          # 模块化核心框架
│   │   ├── IModule.cs                        #   模块接口定义
│   │   ├── ModuleDescriptor.cs               #   模块元数据描述符
│   │   ├── ModuleRegistry.cs                 #   模块注册表 & 依赖图
│   │   └── ModuleBoundaryVerifier.cs         #   模块边界验证器
│   │
│   ├── DotNetModulith.ServiceDefaults/       # 共享服务默认配置
│   │   └── Extensions.cs                     #   OpenTelemetry、健康检查、服务发现
│   │
│   ├── DotNetModulith.Api/                   # API 主机
│   │   └── Program.cs                        #   CAP、Mediator、模块注册、端点映射
│   │
│   ├── DotNetModulith.AppHost/               # Aspire 编排主机
│   │   └── Program.cs                        #   PostgreSQL、RabbitMQ、OpenObserve、迁移服务
│   │
│   ├── DotNetModulith.MigrationService/      # 数据库迁移后台服务
│   │
│   └── DotNetModulith.Modules/               # 业务模块
│       ├── Orders/                           #   订单模块
│       │   ├── Api/                          #     Minimal API 端点
│       │   ├── Application/                  #     命令、查询、事件发布器、订阅者
│       │   ├── Domain/                       #     聚合根、值对象、领域事件、仓储接口
│       │   └── Infrastructure/               #     EF Core 上下文、仓储实现
│       ├── Inventory/                        #   库存模块（同上分层）
│       ├── Payments/                         #   支付模块（同上分层）
│       └── Notifications/                    #   通知模块（事件订阅者）
│
├── tests/
│   ├── DotNetModulith.Modules.Orders.Tests/  # 订单领域单元测试
│   ├── DotNetModulith.IntegrationTests/      # 集成测试（Testcontainers）
│   └── DotNetModulith.ArchitectureTests/     # 架构测试（ArchUnitNET）
│
├── Directory.Build.props                     # 全局构建属性
├── Directory.Packages.props                  # 中央包版本管理
├── global.json                               # SDK 版本锁定
└── .editorconfig                             # 代码格式规范
```

## 模块依赖关系

```
Inventory ←── Orders ←── Payments
                ↑           ↑
                └───────────┘
                     ↓
              Notifications
```

| 模块              | 发布事件                                           | 订阅事件                                       |
| ----------------- | -------------------------------------------------- | ---------------------------------------------- |
| **Orders**        | OrderCreated, OrderPaid, OrderCancelled            | StockReserved, PaymentCompleted                |
| **Inventory**     | StockReserved, StockInsufficient, StockReplenished | OrderCreated                                   |
| **Payments**      | PaymentCompleted, PaymentFailed                    | OrderCreated                                   |
| **Notifications** | —                                                  | OrderCreated, PaymentCompleted, OrderCancelled |

## 快速开始

### 前置条件

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)（用于 Aspire 编排和 Testcontainers）
- 工作负载：`aspire`（`dotnet workload install aspire`）

### 启动开发环境

```bash
# 克隆仓库
git clone https://github.com/ZUOXIANGE/dotnet-modulith.git
cd dotnet-modulith

# 还原工具（Husky.Net 等）
dotnet tool restore

# 安装 Husky Git 钩子
dotnet husky install

# 使用 Aspire 启动（自动拉取 PostgreSQL 和 RabbitMQ 容器）
dotnet run --project src/DotNetModulith.AppHost
```

Aspire 将自动：
1. 启动 PostgreSQL、RabbitMQ 和 OpenObserve 容器
2. 运行数据库迁移服务
3. 启动 API 主机

启动后可访问：
- **API**：`https://localhost:7001`（端口以 Aspire 输出为准）
- **Scalar API 文档**：`https://localhost:7001/scalar/v1`
- **CAP Dashboard**：`https://localhost:7001/cap-dashboard`
- **OpenObserve**：`http://localhost:5080`（账号：`admin@modulith.local`，密码：`Modulith@2026`）
- **Aspire Dashboard**：`http://localhost:15000`

### 不使用 Aspire 启动

如果已有本地 PostgreSQL 和 RabbitMQ 实例：

```bash
# 1. 启动基础设施容器
docker run -d --name modulith-postgres -p 5432:5432 \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=modulith_orders \
  postgres:17

docker run -d --name modulith-rabbitmq -p 5672:5672 -p 15672:15672 \
  rabbitmq:4-management

docker run -d --name modulith-openobserve -p 5080:5080 -p 5081:5081 \
  -e ZO_ROOT_USER_EMAIL=admin@modulith.local \
  -e ZO_ROOT_USER_PASSWORD=Modulith@2026 \
  -e ZO_DATA_DIR=/data \
  -v modulith-openobserve-data:/data \
  public.ecr.aws/zinclabs/openobserve

# 2. 创建数据库
docker exec modulith-postgres psql -U postgres -c "CREATE DATABASE modulith_inventory;"
docker exec modulith-postgres psql -U postgres -c "CREATE DATABASE modulith_payments;"
docker exec modulith-postgres psql -U postgres -c "CREATE DATABASE modulith_cap;"

# 3. 运行迁移
dotnet ef database update --project src/DotNetModulith.Modules.Orders \
  --startup-project src/DotNetModulith.Api \
  --context OrdersDbContext
dotnet ef database update --project src/DotNetModulith.Modules.Inventory \
  --startup-project src/DotNetModulith.Api \
  --context InventoryDbContext
dotnet ef database update --project src/DotNetModulith.Modules.Payments \
  --startup-project src/DotNetModulith.Api \
  --context PaymentsDbContext

# 4. 启动 API
dotnet run --project src/DotNetModulith.Api

# 5. 运行测试脚本验证
pwsh -ExecutionPolicy Bypass -File test-api.ps1
```

## API 端点

### 订单模块

| 方法 | 路径                            | 说明         |
| ---- | ------------------------------- | ------------ |
| POST | `/api/orders`                   | 创建订单     |
| POST | `/api/orders/{orderId}/confirm` | 确认订单     |
| GET  | `/api/orders/{orderId}`         | 查询订单详情 |
| DELETE | `/api/orders/{orderId}/cache` | 手动清理订单缓存 |

## 多级缓存（FusionCache）示例

项目已在订单查询链路集成 FusionCache，多级缓存策略如下：

- L1：进程内内存缓存（默认 30 秒）
- L2：Redis 分布式缓存（默认 5 分钟）
- 查询入口：`GET /api/orders/{orderId}`（`GetOrderQueryHandler` 使用 `IFusionCache.GetOrSetAsync`）
- 失效入口：订单创建/确认后自动清理指定订单缓存；可调用 `DELETE /api/orders/{orderId}/cache` 手动清理

默认配置位于 `appsettings*.json`：

```json
"ConnectionStrings": {
  "redis": "localhost:6379"
},
"Caching": {
  "Orders": {
    "Duration": "00:05:00",
    "MemoryCacheDuration": "00:00:30",
    "DistributedCacheDuration": "00:05:00",
    "EnableFailSafe": true,
    "FailSafeMaxDuration": "00:30:00"
  }
}
```

可通过以下步骤验证缓存行为：

1. 创建订单：`POST /api/orders`
2. 连续两次读取：`GET /api/orders/{orderId}`（第二次应命中缓存）
3. 调用确认接口：`POST /api/orders/{orderId}/confirm`
4. 再次读取订单：`GET /api/orders/{orderId}`（应触发缓存重建）

### 库存模块

| 方法 | 路径                                          | 说明         |
| ---- | --------------------------------------------- | ------------ |
| GET  | `/api/inventory/stocks/{productId}`           | 查询库存     |
| POST | `/api/inventory/stocks`                       | 创建库存记录 |
| POST | `/api/inventory/stocks/{productId}/replenish` | 补充库存     |

### 模块管理

| 方法 | 路径                  | 说明                               |
| ---- | --------------------- | ---------------------------------- |
| GET  | `/api/modules`        | 获取模块列表                       |
| GET  | `/api/modules/graph`  | 获取模块依赖图（Mermaid/PlantUML） |
| GET  | `/api/modules/verify` | 验证模块边界                       |

## 事件流示例

以"创建订单"为例的完整事件流：

```
1. POST /api/orders → CreateOrderCommand
2. Order.Create() → OrderCreatedDomainEvent
3. DomainEventToIntegrationEventPublisher → OrderCreatedIntegrationEvent (via CAP/RabbitMQ)
4. Inventory.OrderEventSubscriber → 预留库存 → StockReservedIntegrationEvent
5. Payments.OrderEventSubscriber → 处理支付 → PaymentCompletedIntegrationEvent
6. Orders.PaymentEventSubscriber → 标记订单已支付
7. Notifications.NotificationEventSubscriber → 发送通知
```

## 分布式链路追踪

项目集成了 OpenTelemetry + OpenObserve，可观测完整的跨模块事件流链路。

### 链路追踪架构

```
API 请求 → ASP.NET Core Instrumentation
         → Mediator Command/Query Handler (自定义 Span)
         → 领域事件发布 (自定义 Span)
         → CAP Outbox → RabbitMQ → CAP Consumer
         → 事件订阅者处理 (自定义 Span, ActivityKind.Consumer)
         → OTLP Export → OpenObserve
```

### 运行测试脚本

项目提供了 `test-api.ps1` 脚本，可一键验证完整的业务流程和链路追踪：

```powershell
# 确保 Docker 容器已启动
docker run -d --name modulith-postgres -p 5432:5432 -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=modulith_orders postgres:17
docker run -d --name modulith-rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:4-management
docker run -d --name modulith-openobserve -p 5080:5080 -p 5081:5081 \
  -e ZO_ROOT_USER_EMAIL=admin@modulith.local \
  -e ZO_ROOT_USER_PASSWORD=Modulith@2026 \
  public.ecr.aws/zinclabs/openobserve

# 启动 API
dotnet run --project src/DotNetModulith.Api

# 在另一个终端运行测试脚本
pwsh -ExecutionPolicy Bypass -File test-api.ps1
```

### 测试脚本输出示例

```
=== Step 1: Create Stock Records ===
[POST] Create PROD-001 stock (qty: 100)          → ✅ 201
[POST] Create PROD-002 stock (qty: 200)          → ✅ 201

=== Step 2: Query Stocks ===
[GET] Query PROD-001 stock                        → ✅ { availableQuantity: 100, reservedQuantity: 0 }

=== Step 3: Create Order (Triggers Full Event Flow) ===
[POST] Create order: CUST-2026-001               → ✅ { orderId: "d1e984e2-..." }

=== Step 4: Wait for async event processing ===
Waiting 8 seconds for CAP/RabbitMQ event processing...

=== Step 5: Verify Stock Reserved ===
[GET] PROD-001 stock (expected: available=98)     → ✅ { availableQuantity: 98, reservedQuantity: 2 }
[GET] PROD-002 stock (expected: available=197)    → ✅ { availableQuantity: 197, reservedQuantity: 3 }
```

### 在 OpenObserve 中查看链路追踪

1. 访问 [http://localhost:5080](http://localhost:5080)
2. 使用 `admin@modulith.local` / `Modulith@2026` 登录
3. 进入 **Traces** 页面，选择 `default` 组织和 `dotnet_modulith` stream
4. 可查看以下关键 Span：

| Span 名称                                    | 类型       | 说明                           |
| -------------------------------------------- | ---------- | ------------------------------ |
| `POST /api/orders/`                          | Server     | HTTP 入口 Span                 |
| `CreateOrder`                                | Internal   | Mediator 命令处理              |
| `Order.Create`                               | Internal   | 聚合根创建 & 领域事件生成      |
| `PublishIntegrationEvent.OrderCreatedDomainEvent` | Internal | CAP Outbox 写入 & RabbitMQ 发布 |
| `HandleOrderCreated`                         | Consumer   | 库存模块消费事件               |
| `ReserveStock`                               | Internal   | 库存预留业务逻辑               |
| `HandleOrderCreated_ProcessPayment`          | Consumer   | 支付模块消费事件               |
| `HandlePaymentCompleted`                     | Consumer   | 订单模块消费支付完成事件       |
| `SendOrderCreatedNotification`               | Consumer   | 通知模块发送订单确认通知       |
| `SendPaymentCompletedNotification`           | Consumer   | 通知模块发送支付回执通知       |

### 链路追踪数据示例

通过 OpenObserve API 查询到的实际 trace spans：

```
DotNetModulith.Api | POST /api/orders/                    | dur: 913278ns
DotNetModulith.Api | CreateOrder                         | dur: 886524ns
DotNetModulith.Api | Order.Create                        | dur: 4730ns
DotNetModulith.Api | PublishIntegrationEvent...           | dur: 421055ns
DotNetModulith.Api | GET /api/inventory/stocks/{productId} | dur: 35357ns
```

### 自定义 Span

各模块通过 `ActivitySource` 创建自定义 Span，携带业务标签：

```csharp
using var activity = ActivitySource.StartActivity("HandleOrderCreated", ActivityKind.Consumer);
activity?.SetTag("modulith.event_type", "OrderCreatedIntegrationEvent");
activity?.SetTag("modulith.order_id", @event.OrderId);
```

### 自定义指标

各模块通过 `Meter` 和 `Counter` 记录业务指标：

```csharp
private static readonly Counter<long> EventsConsumed = Meter.CreateCounter<long>(
    "modulith.inventory.events.consumed",
    unit: "{event}",
    description: "Number of events consumed by the Inventory module");

EventsConsumed.Add(1, new KeyValuePair<string, object?>("modulith.event_type", "OrderCreatedIntegrationEvent"));
```

## 测试

```bash
# 运行所有测试
dotnet test

# 仅运行单元测试
dotnet test tests/DotNetModulith.Modules.Orders.Tests

# 仅运行架构测试（不需要 Docker）
dotnet test tests/DotNetModulith.ArchitectureTests

# 运行集成测试（需要 Docker）
dotnet test tests/DotNetModulith.IntegrationTests
```

### 测试策略

| 层级         | 框架                        | 说明                               |
| ------------ | --------------------------- | ---------------------------------- |
| 领域单元测试 | xUnit v3 + FluentAssertions | 验证聚合根业务规则和状态流转       |
| 架构测试     | ArchUnitNET                 | 验证模块边界、DDD 分层依赖规则     |
| 集成测试     | xUnit v3 + Testcontainers   | 使用真实 PostgreSQL 容器验证持久化 |
| API 测试     | WebApplicationFactory       | 验证 HTTP 端点和健康检查           |

## 代码规范

### 提交信息

遵循 [Conventional Commits](https://www.conventionalcommits.org/) 规范（由 Husky.Net 自动验证）：

```
feat(orders): add order cancellation endpoint
fix(inventory): correct stock reservation logic
chore: update dependencies
```

### 提交前检查

Husky.Net 在 `pre-commit` 阶段自动执行：
- **代码格式检查**：`dotnet format --verify-no-changes`
- **构建验证**：`dotnet build --no-restore`

## 架构决策

| 决策     | 选择                   | 原因                                             |
| -------- | ---------------------- | ------------------------------------------------ |
| API 风格 | Minimal API            | 与模块化架构天然契合，零继承负担，AOT 兼容       |
| Mediator | martinothamar/Mediator | 源生成器零反射，编译时安全，性能优于 MediatR     |
| 对象映射 | Riok.Mapperly          | 源生成器零反射，编译时类型安全，性能最优         |
| 事件总线 | DotNetCore.CAP         | 内置 Outbox 模式，确保消息可靠性，支持 Dashboard |
| API 文档 | Scalar                 | 现代化 UI，比 Swagger 更好的开发者体验           |
| 数据库   | 每模块独立 DbContext   | 模块数据隔离，便于未来拆分                       |
| 可观测性 | OpenObserve            | 存储成本仅为 Elasticsearch 的 1/140，原生 OTLP 支持，集成日志/追踪/指标 |

## 许可证

[Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0)
