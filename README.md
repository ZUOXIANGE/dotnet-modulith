# DotNetModulith

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-6A1B9A?logo=opentelemetry)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

基于 ASP.NET Core 10 的模块化单体示例项目，借鉴 [Spring Modulith](https://spring.io/projects/spring-modulith) 设计理念，结合 DDD、事件驱动、CQRS 与可观测性，演示一个可演进到微服务的工程实践。

## 核心能力
- 模块化单体：模块边界清晰，支持运行时与架构测试校验
- DDD + CQRS：聚合根、值对象、领域事件与命令/查询分离
- 事件驱动：基于 CAP + RabbitMQ 的可靠事件发布订阅（含 Outbox）
- 定时调度：基于 TickerQ 的持久化任务与 Dashboard
- 多级缓存：FusionCache（L1 内存 + L2 Redis）
- 统一可观测：OpenTelemetry + OpenObserve，支持 Logs/Traces/Metrics
- 结构化日志：Serilog JSON（控制台 + 文件），并异步写入

## 技术栈
| 类别   | 技术                                                       |
| ------ | ---------------------------------------------------------- |
| 运行时 | .NET 10                                                    |
| Web    | ASP.NET Core Controllers + OpenAPI                         |
| 数据库 | PostgreSQL + EF Core 10                                    |
| 消息   | RabbitMQ + DotNetCore.CAP                                  |
| 调度   | TickerQ + TickerQ.EntityFrameworkCore + TickerQ.Dashboard  |
| 缓存   | FusionCache + Redis                                        |
| 观测   | OpenTelemetry + OpenObserve + OTEL Collector               |
| 日志   | Serilog（Async + JSON）                                    |
| 文档   | Scalar.AspNetCore                                          |
| 编排   | .NET Aspire                                                |
| 测试   | xUnit v3 + FluentAssertions + Testcontainers + ArchUnitNET |

## 项目结构
```text
src/
  DotNetModulith.Api/                 # API 主机（Program.cs）
  DotNetModulith.AppHost/             # Aspire 编排入口
  DotNetModulith.ServiceDefaults/     # OTel/健康检查/服务发现默认配置
  DotNetModulith.MigrationService/    # 迁移服务
  DotNetModulith.Abstractions/        # 共享抽象与事件契约
  DotNetModulith.ModulithCore/        # 模块注册与边界验证核心
  DotNetModulith.Modules.*            # 业务模块（Orders/Inventory/Payments/Notifications）
scripts/
  test-api.ps1                        # 常用 API 联调与链路验证脚本
tests/
  DotNetModulith.Modules.Orders.Tests/
  DotNetModulith.IntegrationTests/
  DotNetModulith.ArchitectureTests/
```

## 数据库拓扑
- 业务数据库：`modulithdb`
- 调度数据库：`tickerqdb`
- 设计原则：TickerQ 使用独立数据库，不与订单、库存、支付等业务表混放
- 部署建议：生产环境可使用同一 PostgreSQL 实例下的不同数据库，也可完全拆分为独立实例

当前实现中：
- CAP Outbox、业务模块实体和读写模型使用 `modulithdb`
- TickerQ 的时间任务、Cron 任务和执行状态使用 `tickerqdb`
- `MigrationService` 会同时迁移业务库与调度库

## 快速启动（推荐：Aspire）
### 前置条件
- .NET 10 SDK
- Docker Desktop
- 执行 `dotnet restore` 还原 NuGet 包
- 无需安装 Aspire workload；Aspire 现已通过 `Aspire.AppHost.Sdk` 和相关 NuGet 包直接随项目还原

### 启动
```bash
dotnet run --project src/DotNetModulith.AppHost
```

启动后可在 Aspire Dashboard 查看各服务与端点（默认 `http://localhost:15000`）。

Aspire 启动后会自动准备：
- `modulithdb`：业务数据库
- `tickerqdb`：TickerQ 调度数据库
- `rabbitmq`、`redis`、`openobserve`、`otel-collector`

推荐做法：
- 统一通过 `DotNetModulith.AppHost` 启动应用与全部依赖
- 通过 Aspire Dashboard 查看 `api`、`jobhost`、数据库、消息队列、缓存与可观测组件状态
- 仅使用项目内的 NuGet 包版本管理 Aspire；升级时保持 `Aspire.AppHost.Sdk` 与 `Directory.Packages.props` 中的 Aspire 包版本一致
- 不再维护独立启动脚本，避免本地手工环境与实际编排环境不一致
- `api`、`jobhost`、`openobserve`、`pgadmin`、`rabbitmq-management` 等访问地址以 Aspire Dashboard 中显示的实际端点为准

常用入口：
- `Aspire Dashboard`：默认 `http://localhost:15000`
- `API Base URL`：在 Aspire Dashboard 的 `api` 资源详情中查看
- `JobHost Base URL`：在 Aspire Dashboard 的 `jobhost` 资源详情中查看
- `OpenObserve`：在 Aspire Dashboard 的 `openobserve` 资源详情中查看
- `RabbitMQ Management`：在 Aspire Dashboard 的 `rabbitmq-management` 端点中查看
- `PgAdmin`：在 Aspire Dashboard 的 `pgadmin` 资源详情中查看

常用脚本：
- API 联调脚本：`pwsh ./scripts/test-api.ps1`

## API 文档
- Scalar UI：`{api-base-url}/scalar/v1`
- OpenAPI JSON：`{api-base-url}/openapi/v1.json`
- 接口说明来自 XML 注释（Controller、Action、请求/响应模型）
- OpenAPI 描述中已补充 `Bearer` 使用说明，便于联调时复制请求头

其中：
- `{api-base-url}` 表示 Aspire Dashboard 中 `api` 资源暴露出来的 HTTP 地址
- `{jobhost-base-url}` 表示 Aspire Dashboard 中 `jobhost` 资源暴露出来的 HTTP 地址
- `TickerQ Dashboard`：`{jobhost-base-url}/tickerq-dashboard`
- `CAP Dashboard`：`{api-base-url}/cap-dashboard`

Bearer 授权使用方式：

```text
Authorization: Bearer {access_token}
```

联调建议：
- 先调用 `POST /api/auth/login` 获取 `accessToken`
- 之后访问受保护接口时，在请求头中附带 `Authorization: Bearer {accessToken}`
- 可优先联调 `Users`、`Roles` 等受保护接口验证权限链路

## 可观测性验证（Logs + Traces）
### 触发请求
```bash
curl {api-base-url}/alive
curl {api-base-url}/ready
curl {api-base-url}/startup
curl {api-base-url}/health
curl {api-base-url}/api/modules
curl {api-base-url}/api/modules/graph
curl {api-base-url}/api/modules/verify
```

### 健康探针说明（K8S 推荐）
- `/alive`：Liveness Probe，仅检查进程存活
- `/startup`：Startup Probe，应用启动完成前返回非健康状态
- `/ready`：Readiness Probe，包含依赖连通性与应用生命周期状态
- `/health`：`/ready` 的别名，便于兼容现有脚本

### 在 OpenObserve 查看
- 地址：在 Aspire Dashboard 的 `openobserve` 资源详情中查看
- 账号/密码：使用你本地环境变量中配置的凭据
- `Logs` 页面选择 stream：`dotnet_modulith`
- `Traces` 页面选择 stream：`dotnet_modulith`

你可以重点观察这些信号：
- `DotNetModulith.Modules.Inventory` 的低库存扫描 Span 与指标
- `DotNetModulith.Modules.Notifications` 的库存预警通知日志与通知计数
- `TickerQ` 的调度执行链路

## 定时任务
- `Inventory.LowStockAlertScan`：每 5 分钟扫描一次低库存
- 配置节点：`InventoryAlert`
- `Threshold`：低库存阈值，默认 `10`
- `BatchSize`：单次扫描最大处理数，默认 `100`
- 宿主进程：`jobhost`
- 任务看板：`{jobhost-base-url}/tickerq-dashboard`
- 事件链路：`TickerQ` 扫描 -> `modulith.inventory.LowStockDetectedIntegrationEvent` -> `Notifications` 模块发送消息通知

## 接口响应约定
- 所有接口统一返回 `ApiResponse<T>` 结构
- 成功/失败均返回 `HTTP 200`，通过 `code` 区分业务结果
- 控制器使用 `[ApiController]`，采用 ASP.NET Core 原生模型绑定与自动模型验证
- 请求 DTO 使用 `DataAnnotations`（如 `[Required]`、`[StringLength]`、`[Range]`）定义参数约束
- 参数绑定来源遵循原生规则：路由参数来自路径、复杂对象默认来自请求体（也可显式 `[FromBody]`）
- 模型校验失败统一由 `InvalidModelStateResponseFactory` 转换为 `ApiResponse.Failure("validation failed", ApiCodes.Common.ValidationFailed, ...)`
- 业务异常通过 `BusinessException` 抛出并由全局异常处理中间件统一转换

示例：

```json
{
  "msg": "success",
  "code": 200,
  "data": {}
}
```

## 规范与文档
- 认证鉴权与 RBAC 说明见：[docs/auth-rbac.md](docs/auth-rbac.md)
- 对外错误码与统一响应约定见：[docs/api-error-codes.md](docs/api-error-codes.md)
- 开发规范与命名约定见：[docs/development-standards.md](docs/development-standards.md)
- 数据库实体命名约定：统一使用 `*Entity` 后缀

## 运行测试
```bash
dotnet test
dotnet test tests/DotNetModulith.Modules.Orders.Tests
dotnet test tests/DotNetModulith.ArchitectureTests
dotnet test tests/DotNetModulith.IntegrationTests
```

## 许可证
[MIT](https://opensource.org/licenses/MIT)
