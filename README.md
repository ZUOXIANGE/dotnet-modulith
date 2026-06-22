# DotNetModulith — 图书馆管理系统

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-6A1B9A?logo=opentelemetry)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

基于 ASP.NET Core 10 的模块化单体（Modulith）图书馆管理系统，借鉴 [Spring Modulith](https://spring.io/projects/spring-modulith) 设计理念，结合 DDD、事件驱动、CQRS 与 OpenTelemetry 可观测性，演示一个可演进到微服务的工程实践。

## 业务场景

系统覆盖图书馆日常运营的完整流程：图书编目、读者管理、借阅归还、预约排队、逾期罚款、通知推送、统计报表。

## 核心能力

- 模块化单体：模块边界清晰，支持运行时与架构测试校验
- DDD + CQRS：聚合根、领域事件与命令/查询分离
- 事件驱动：CAP + RabbitMQ 可靠事件发布订阅（含 Outbox）
- 定时调度：TickerQ 持久化任务 + Dashboard（独立数据库 `tickerqdb`）
- 认证鉴权：JWT Bearer + RBAC（用户-角色-权限三层模型）
- 多级缓存：FusionCache（L1 内存 + L2 Redis）
- 对象映射：Mapperly 编译时映射，避免运行时反射开销
- 文件存储：RustFS（S3 兼容）+ 直传/签名上传
- 统一可观测：OpenTelemetry + OpenObserve，支持 Logs/Traces/Metrics
- 结构化日志：Serilog JSON（控制台 + 文件），异步写入

## 技术栈

| 类别     | 技术                                                       |
| -------- | ---------------------------------------------------------- |
| 运行时   | .NET 10                                                    |
| Web      | ASP.NET Core Controllers + OpenAPI + Vue 3 + Naive UI      |
| 认证鉴权 | JWT Bearer + RBAC（会话表 + TokenVersion 双重失效）        |
| 数据库   | PostgreSQL + EF Core 10                                    |
| 消息     | RabbitMQ + DotNetCore.CAP（含 Outbox）                     |
| 调度     | TickerQ + TickerQ.EntityFrameworkCore + Dashboard          |
| 缓存     | FusionCache + Redis                                        |
| 对象映射 | Riok.Mapperly（编译时）                                    |
| 对象存储 | RustFS（S3-compatible） + AWS SDK for .NET                 |
| 观测     | OpenTelemetry + OpenObserve + OTEL Collector               |
| 日志     | Serilog（Async + JSON）                                    |
| 文档     | Scalar.AspNetCore                                          |
| 编排     | .NET Aspire                                                |
| 测试     | xUnit v3 + FluentAssertions + Testcontainers + ArchUnitNET |

## 项目结构

```text
src/
  DotNetModulith.Api/                       # API 主机（Program.cs、全局中间件、种子数据）
  DotNetModulith.AppHost/                   # Aspire 编排入口
  DotNetModulith.JobHost/                   # 定时任务宿主（TickerQ）
  DotNetModulith.MigrationService/          # 数据库迁移服务
  DotNetModulith.ServiceDefaults/           # OTel/健康检查/服务发现默认配置
  DotNetModulith.Abstractions/              # 共享抽象、事件契约、统一响应模型
  DotNetModulith.ModulithCore/              # 模块注册、边界验证、依赖图
  DotNetModulith.Modules.Users/             # 用户与认证模块（后台管理员）
  DotNetModulith.Modules.Books/             # 图书与分类管理模块
  DotNetModulith.Modules.Members/           # 会员管理模块（读者用户）
  DotNetModulith.Modules.Borrowing/         # 借阅管理模块
  DotNetModulith.Modules.Reservation/       # 预约管理模块
  DotNetModulith.Modules.Fines/             # 罚款管理模块
  DotNetModulith.Modules.Notifications/     # 通知管理模块
  DotNetModulith.Modules.Reports/           # 报表统计模块
  DotNetModulith.Modules.Storage/           # 文件存储模块
frontend/
  src/views/                                # Vue 3 前端页面
scripts/
  test-api.ps1                              # API 联调脚本
  test-trace-demo.ps1                       # 链路追踪演示脚本
tests/
  DotNetModulith.ArchitectureTests/         # 架构测试（模块边界、依赖方向）
  DotNetModulith.IntegrationTests/          # 集成测试
```

## 业务模块

| 模块          | 功能描述                               | 关键实体        |
| ------------- | -------------------------------------- | --------------- |
| Users         | 后台管理员登录、用户/角色/权限管理     | User, Role      |
| Books         | 图书编目、分类管理、库存管理           | Book, Category  |
| Members       | 读者会员注册、等级管理、状态管理       | Member          |
| Borrowing     | 图书借阅、归还、续借、丢失处理         | BorrowingRecord |
| Reservation   | 图书预约、排队、取消、过期处理         | Reservation     |
| Fines         | 逾期罚款、缴纳、豁免                   | Fine            |
| Notifications | 借阅到期、逾期提醒、预约可借、罚款通知 | Notification    |
| Reports       | 借阅概要、热门图书、逾期报告、借阅趋势 | -               |
| Storage       | 文件上传、下载、签名上传               | -               |

## 数据库拓扑

- 业务数据库：`modulithdb`（所有业务模块共用，通过 Schema 隔离）
- 调度数据库：`tickerqdb`（TickerQ 独立数据库）
- 设计原则：TickerQ 使用独立数据库，不与业务表混放
- CAP Outbox、业务模块实体和读写模型使用 `modulithdb`
- TickerQ 的时间任务、Cron 任务和执行状态使用 `tickerqdb`
- `MigrationService` 会同时迁移业务库与调度库

## 快速启动

### 前置条件

- .NET 10 SDK
- Docker Desktop
- Node.js 18+

### 启动后端

```bash
dotnet restore
dotnet run --project src/DotNetModulith.AppHost
```

启动后 Aspire Dashboard 默认可在 `http://localhost:15000` 访问。

Aspire 自动启动的依赖：
- `modulithdb`（PostgreSQL 业务库）
- `tickerqdb`（TickerQ 调度库）
- `rabbitmq`（消息队列）
- `redis`（缓存）
- `rustfs`（S3 兼容存储）
- `openobserve`（可观测平台）
- `otel-collector`（遥测收集器）

常用入口（以 Aspire Dashboard 中显示的实际端点为准）：
- `Aspire Dashboard`：默认 `http://localhost:15000`
- `API Base URL`：在 `api` 资源详情中查看
- `Scalar API 文档`：`{api-base-url}/scalar/v1`
- `TickerQ Dashboard`：`{jobhost-base-url}/tickerq-dashboard`
- `CAP Dashboard`：`{api-base-url}/cap-dashboard`
- `OpenObserve`：在 `openobserve` 资源详情中查看
- `RabbitMQ Management`：在 `rabbitmq-management` 端点中查看
- `PgAdmin`：在 `pgadmin` 资源详情中查看

### 启动前端

```bash
cd frontend
npm install
npm run dev
```

前端开发服务器默认运行在 `http://localhost:5173`，API 请求通过 Vite 代理转发到后端。

### 默认账号

系统首次启动时自动种子以下后台登录账号：

| 角色       | 用户名      | 密码             | 说明                          |
| ---------- | ----------- | ---------------- | ----------------------------- |
| 系统管理员 | `admin`     | `Admin@123456`   | 拥有全部权限                  |
| 图书管理员 | `librarian` | `Library@123456` | 图书/借阅/会员/罚款等管理权限 |

## 接口响应约定

所有接口统一返回 `ApiResponse<T>` 结构，成功/失败均返回 `HTTP 200`，通过 `code` 区分业务结果：

```json
{
  "msg": "success",
  "code": 200,
  "data": {}
}
```

- 控制器使用 `[ApiController]`，采用 ASP.NET Core 原生模型绑定与自动模型验证
- 请求 DTO 使用 `DataAnnotations`（`[Required]`、`[StringLength]`、`[Range]` 等）
- 模型校验失败统一转换为 `ApiResponse.Failure("validation failed", ApiCodes.Common.ValidationFailed, ...)`
- 业务异常通过 `BusinessException` 抛出并由全局异常处理中间件统一转换

## 认证鉴权

- 认证流程：`POST /api/auth/login` 获取 JWT → 后续请求携带 `Authorization: Bearer {token}`
- RBAC 权限模型：用户 → 角色 → 权限点（如 `books.view`、`members.manage`）
- 令牌失效：支持会话表撤销 + TokenVersion 双重失效机制
- 详细说明见 [docs/auth-rbac.md](docs/auth-rbac.md)

## 模块间通信

| 模式             | 适用场景               | 实现                  |
| ---------------- | ---------------------- | --------------------- |
| 事件驱动（异步） | 最终一致性、跨模块解耦 | CAP + RabbitMQ Outbox |
| 同步调用         | 实时校验、事务内操作   | DI 注入公开 API 接口  |

详细规则见 [docs/module-communication.md](docs/module-communication.md)

## 定时任务

| 任务                  | 所属模块    | 说明                     |
| --------------------- | ----------- | ------------------------ |
| OverdueDetectionJob   | Borrowing   | 定期扫描逾期借阅记录     |
| ReservationExpiryJob  | Reservation | 定期清理过期预约         |
| OverdueFineSubscriber | Fines       | 监听逾期事件自动生成罚款 |

统一通过 `JobHost` 进程调度，任务看板：`{jobhost-base-url}/tickerq-dashboard`

## 可观测性

### 健康探针（K8S 推荐）

- `/alive`：Liveness Probe，仅检查进程存活
- `/startup`：Startup Probe，应用启动完成前返回非健康状态
- `/ready`：Readiness Probe，包含依赖连通性与应用生命周期状态
- `/health`：`/ready` 的别名

### 在 OpenObserve 查看

- 地址：在 Aspire Dashboard 的 `openobserve` 资源详情中查看
- `Logs` 页面选择 stream：`dotnet_modulith`
- `Traces` 页面选择 stream：`dotnet_modulith`

### 链路验证

```bash
pwsh ./scripts/test-trace-demo.ps1
```

验证 API → DB → Cache → Queue → Subscriber 完整链路。

## 运行测试

```bash
dotnet test
dotnet test tests/DotNetModulith.ArchitectureTests
dotnet test tests/DotNetModulith.IntegrationTests
```

## 规范与文档

| 文档                                                           | 说明                     |
| -------------------------------------------------------------- | ------------------------ |
| [docs/project-overview.md](docs/project-overview.md)           | 系统功能说明与使用指南   |
| [docs/auth-rbac.md](docs/auth-rbac.md)                         | 认证鉴权与 RBAC 详细说明 |
| [docs/api-error-codes.md](docs/api-error-codes.md)             | API 错误码对照表         |
| [docs/development-standards.md](docs/development-standards.md) | 项目开发规范             |
| [docs/module-communication.md](docs/module-communication.md)   | 模块间通信规范           |
| [docs/scheduled-jobs.md](docs/scheduled-jobs.md)               | 定时任务开发说明         |
| [src/README.md](src/README.md)                                 | 后端源码结构说明         |

## 许可证

[MIT](https://opensource.org/licenses/MIT)
