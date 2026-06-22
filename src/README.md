# 后端源码结构

本文档描述 `src/` 目录下各项目的职责与分层约定。

## 项目分类

```text
src/
├── Core/                                    # 核心基础设施
│   ├── DotNetModulith.Abstractions/         # 共享抽象、事件契约、统一响应模型
│   ├── DotNetModulith.ModulithCore/         # 模块注册、边界验证、依赖图
│   └── DotNetModulith.ServiceDefaults/      # OTel/健康检查/服务发现默认配置
├── Host/                                    # 宿主进程
│   ├── DotNetModulith.Api/                  # API 主机（Controllers、中间件、种子数据）
│   ├── DotNetModulith.AppHost/              # Aspire 编排入口
│   ├── DotNetModulith.JobHost/              # 定时任务宿主（TickerQ）
│   └── DotNetModulith.MigrationService/     # 数据库迁移服务
└── Modules/                                 # 业务模块
    ├── DotNetModulith.Modules.Users/        # 用户与认证模块
    ├── DotNetModulith.Modules.Books/        # 图书与分类管理模块
    ├── DotNetModulith.Modules.Members/      # 会员管理模块
    ├── DotNetModulith.Modules.Borrowing/    # 借阅管理模块
    ├── DotNetModulith.Modules.Reservation/  # 预约管理模块
    ├── DotNetModulith.Modules.Fines/        # 罚款管理模块
    ├── DotNetModulith.Modules.Notifications/# 通知管理模块
    ├── DotNetModulith.Modules.Reports/      # 报表统计模块
    └── DotNetModulith.Modules.Storage/      # 文件存储模块
```

## Core 项目

### DotNetModulith.Abstractions

跨模块共享的基础类型，不依赖任何业务模块。

| 目录             | 内容                                                                              |
| ---------------- | --------------------------------------------------------------------------------- |
| `Authorization/` | 权限码常量定义 (`PermissionCodes`)                                                |
| `Domain/`        | 聚合根基类 (`AggregateRoot`)、实体接口                                            |
| `Events/`        | 领域事件接口 (`IDomainEvent`)、集成事件基类 (`IntegrationEvent`) 及跨模块事件定义 |
| `Exceptions/`    | 业务异常基类 (`BusinessException`)                                                |
| `Results/`       | 统一响应模型 (`ApiResponse<T>`)、业务码 (`ApiCodes`)                              |
| `Validation/`    | 自定义校验特性 (`NotEmptyCollection`、`NotWhiteSpace`)                            |

### DotNetModulith.ModulithCore

模块化治理核心，提供模块注册、边界验证和依赖图能力。

| 文件                                   | 职责                                          |
| -------------------------------------- | --------------------------------------------- |
| `IModule.cs`                           | 模块接口定义（名称、发布/订阅事件、注册服务） |
| `ModuleDescriptor.cs`                  | 模块描述符                                    |
| `ModuleRegistry.cs`                    | 模块注册表（发现与收集）                      |
| `ModuleDependencyGraph.cs`             | 模块依赖图构建                                |
| `ModuleDependencyEdge.cs`              | 模块依赖边                                    |
| `ModuleBoundaryVerifier.cs`            | 模块边界校验（架构测试用）                    |
| `DomainEventDispatcher.cs`             | 领域事件分发器                                |
| `ModuleServiceCollectionExtensions.cs` | 模块注册扩展方法                              |

### DotNetModulith.ServiceDefaults

Aspire 服务默认配置，通过 `AddServiceDefaults()` 统一注入。

- OpenTelemetry（Logs、Traces、Metrics）
- 健康检查端点（`/alive`、`/ready`、`/startup`、`/health`）
- 服务发现配置
- 消息与可观测选项（`MessagingOptions`、`OpenObserveOptions`）

## Host 项目

### DotNetModulith.Api

API 主机进程，负责 HTTP 请求处理与全局管道。

| 目录/文件       | 职责                                   |
| --------------- | -------------------------------------- |
| `Controllers/`  | 全局控制器（健康检查、模块治理）       |
| `Data/`         | 种子数据（`LibraryDataSeeder`）        |
| `HealthChecks/` | 自定义健康检查（TCP 连通性、生命周期） |
| `Program.cs`    | 应用启动入口，注册中间件与模块         |

### DotNetModulith.AppHost

.NET Aspire 编排入口，定义所有服务与依赖资源。

- PostgreSQL（`modulithdb`、`tickerqdb`）
- RabbitMQ
- Redis
- RustFS（S3 兼容存储）
- OpenObserve
- OTEL Collector
- PgAdmin

### DotNetModulith.JobHost

定时任务宿主进程，承载 TickerQ 调度器。

| 目录/文件                                     | 职责                              |
| --------------------------------------------- | --------------------------------- |
| `Infrastructure/TickerQSchedulerDbContext.cs` | TickerQ 调度库上下文              |
| `Infrastructure/TickerQMigrations/`           | TickerQ 数据库迁移                |
| `Program.cs`                                  | 启动入口，配置 TickerQ 与健康检查 |
| `JobHostApplicationExtensions.cs`             | 应用管道扩展                      |
| `JobHostServiceCollectionExtensions.cs`       | 服务注册扩展                      |

### DotNetModulith.MigrationService

数据库迁移服务，启动时自动执行 EF Core 迁移。

- 迁移业务库 `modulithdb` 中各模块的 DbContext
- 迁移调度库 `tickerqdb` 中的 TickerQ DbContext

## 业务模块

每个业务模块遵循统一的分层结构：

```text
DotNetModulith.Modules.{Name}/
├── Api/                           # 接口层（对外公开）
│   ├── Contracts/
│   │   ├── Requests/              # 请求 DTO（*Request）
│   │   └── Responses/             # 响应 DTO（*Response）
│   ├── Controllers/               # API 控制器
│   └── Mappings/                  # Mapperly 映射器（DTO ↔ 内部模型）
├── Application/                   # 应用层
│   ├── Services/                  # 应用服务（内部实现）
│   ├── Jobs/                      # 定时任务（TickerQ Job）
│   ├── Subscribers/               # 集成事件订阅者（CAP Subscriber）
│   ├── *Input.cs                  # 输入模型（DTO）
│   ├── *Details.cs / *ListItem.cs # 内部模型（只读 DTO）
│   └── I*Service.cs               # 公开服务接口（模块间同步调用）
├── Domain/                        # 领域层
│   ├── *Entity.cs                 # 领域实体（聚合根）
│   └── *Status.cs / *Type.cs      # 枚举
├── Infrastructure/                # 基础设施层
│   ├── Configurations/            # EF Core 实体配置
│   ├── Migrations/                # EF Core 迁移
│   └── *DbContext.cs              # 数据库上下文
├── *Module.cs                     # 模块注册（实现 IModule）
├── *Permissions.cs                # 权限码常量
├── *ServiceCollectionExtensions.cs # DI 注册扩展
└── DotNetModulith.Modules.*.csproj
```

### 分层职责

| 层             | 职责                                                          | 可见性                                           |
| -------------- | ------------------------------------------------------------- | ------------------------------------------------ |
| Api            | 接收 HTTP 请求，参数校验，调用应用服务，返回 `ApiResponse<T>` | `public`                                         |
| Application    | 业务用例编排，事务管理，事件发布，模块间同步调用              | `internal`（服务实现）<br>`public`（接口和 DTO） |
| Domain         | 领域实体、值对象、领域行为                                    | `internal`                                       |
| Infrastructure | 数据访问（EF Core DbContext、仓储）、消息订阅、外部服务调用   | `internal`                                       |

### 架构约束

架构测试 (`ArchitectureTests`) 强制执行以下规则：

- 模块间只允许引用 `*.Api` 命名空间，禁止引用 `*.Application`、`*.Domain`、`*.Infrastructure`
- 模块通过 `IModule` 接口声明发布/订阅的集成事件
- 模块依赖方向由 `ModuleDependencyGraph` 构建并可视化

### 模块清单

| 模块          | 项目名                                 | 核心功能                                  |
| ------------- | -------------------------------------- | ----------------------------------------- |
| Users         | `DotNetModulith.Modules.Users`         | 后台管理员认证、用户/角色/权限管理        |
| Books         | `DotNetModulith.Modules.Books`         | 图书编目、分类管理、库存管理              |
| Members       | `DotNetModulith.Modules.Members`       | 读者会员注册、等级管理、状态管理          |
| Borrowing     | `DotNetModulith.Modules.Borrowing`     | 图书借阅、归还、续借、丢失处理、逾期检测  |
| Reservation   | `DotNetModulith.Modules.Reservation`   | 图书预约、排队、取消、过期处理            |
| Fines         | `DotNetModulith.Modules.Fines`         | 逾期罚款、缴纳、豁免                      |
| Notifications | `DotNetModulith.Modules.Notifications` | 事件驱动的通知推送（借阅/逾期/预约/罚款） |
| Reports       | `DotNetModulith.Modules.Reports`       | 跨模块数据聚合与统计报表                  |
| Storage       | `DotNetModulith.Modules.Storage`       | S3 兼容文件存储（上传/下载/签名上传）     |

## 模块间事件流

```
Borrowing ──(BookBorrowed)──▶ Notifications
Borrowing ──(BookReturned)──▶ Reservation ──(ReservationAvailable)──▶ Notifications
Borrowing ──(BookReturned)──▶ Notifications
Borrowing ──(BookOverdue)───▶ Fines ──(FineCreated)──▶ Notifications
Reservation──(ReservationExpired)──▶ Notifications
```

## 测试

```text
tests/
├── DotNetModulith.ArchitectureTests/       # 模块边界、依赖方向、分层约束
└── DotNetModulith.IntegrationTests/        # 端到端集成测试（Testcontainers）
    ├── Fixtures/                           # 测试夹具与工厂
    └── Storage/                            # 存储模块集成测试
```
