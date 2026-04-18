# DotNetModulith

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-6A1B9A?logo=opentelemetry)](https://opentelemetry.io/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

基于 ASP.NET Core 10 的模块化单体示例项目，借鉴 [Spring Modulith](https://spring.io/projects/spring-modulith) 设计理念，结合 DDD、事件驱动、CQRS 与可观测性，演示一个可演进到微服务的工程实践。

## 核心能力
- 模块化单体：模块边界清晰，支持运行时与架构测试校验
- DDD + CQRS：聚合根、值对象、领域事件与命令/查询分离
- 事件驱动：基于 CAP + RabbitMQ 的可靠事件发布订阅（含 Outbox）
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
tests/
  DotNetModulith.Modules.Orders.Tests/
  DotNetModulith.IntegrationTests/
  DotNetModulith.ArchitectureTests/
```

## 快速启动（推荐：Aspire）
### 前置条件
- .NET 10 SDK
- Docker Desktop
- Aspire workload：`dotnet workload install aspire`

### 启动
```bash
dotnet run --project src/DotNetModulith.AppHost
```

启动后可在 Aspire Dashboard 查看各服务与端点（默认 `http://localhost:15000`）。

## API 文档
- Scalar UI：`http://localhost:5000/scalar/v1`
- OpenAPI JSON：`http://localhost:5000/openapi/v1.json`
- 接口说明来自 XML 注释（Controller、Action、请求/响应模型）

## 独立启动（不通过 AppHost）
当你需要单独调试 API 时，可使用以下方式：

### 1) 启动基础依赖
```bash
docker run --name modulith-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=modulith -p 5432:5432 -d postgres:16
docker run --name modulith-rabbitmq -p 5672:5672 -p 15672:15672 -d rabbitmq:3-management
docker run --name modulith-redis -p 6379:6379 -d redis:7
docker run --name modulith-openobserve -p 5080:5080 -e ZO_ROOT_USER_EMAIL=admin@modulith.local -e ZO_ROOT_USER_PASSWORD=Modulith@2026 -d public.ecr.aws/zinclabs/openobserve:latest
```

### 2) 启动 API
```powershell
$env:OpenObserve__Enabled='true'
$env:OpenObserve__Endpoint='http://localhost:5080'
$env:OpenObserve__Organization='default'
$env:OpenObserve__StreamName='dotnet-modulith'
$env:OpenObserve__UserEmail='<your-openobserve-email>'
$env:OpenObserve__UserPassword='<your-openobserve-password>'
$env:ConnectionStrings__modulithdb='Host=localhost;Port=5432;Database=modulith;Username=postgres;Password=postgres'
$env:ConnectionStrings__redis='localhost:6379'
dotnet run --no-launch-profile --urls http://localhost:5000 --project src/DotNetModulith.Api
```

## 可观测性验证（Logs + Traces）
### 触发请求
```bash
curl http://localhost:5000/alive
curl http://localhost:5000/ready
curl http://localhost:5000/startup
curl http://localhost:5000/health
curl http://localhost:5000/api/modules
curl http://localhost:5000/api/modules/graph
curl http://localhost:5000/api/modules/verify
```

### 健康探针说明（K8S 推荐）
- `/alive`：Liveness Probe，仅检查进程存活
- `/startup`：Startup Probe，应用启动完成前返回非健康状态
- `/ready`：Readiness Probe，包含依赖连通性与应用生命周期状态
- `/health`：`/ready` 的别名，便于兼容现有脚本

### 在 OpenObserve 查看
- 地址：`http://localhost:5080`
- 账号/密码：使用你本地环境变量中配置的凭据
- `Logs` 页面选择 stream：`dotnet_modulith`
- `Traces` 页面选择 stream：`dotnet_modulith`

## 主要 API
### Orders
| 方法   | 路径                            | 说明                   |
| ------ | ------------------------------- | ---------------------- |
| POST   | `/api/orders`                   | 创建订单               |
| GET    | `/api/orders/{orderId}`         | 查询订单详情（带缓存） |
| POST   | `/api/orders/{orderId}/confirm` | 确认订单               |
| DELETE | `/api/orders/{orderId}/cache`   | 手动清理订单缓存       |

### Inventory
| 方法 | 路径                                          | 说明     |
| ---- | --------------------------------------------- | -------- |
| GET  | `/api/inventory/stocks/{productId}`           | 查询库存 |
| POST | `/api/inventory/stocks`                       | 创建库存 |
| POST | `/api/inventory/stocks/{productId}/replenish` | 补充库存 |

### Modules
| 方法 | 路径                  | 说明         |
| ---- | --------------------- | ------------ |
| GET  | `/api/modules`        | 模块列表     |
| GET  | `/api/modules/graph`  | 模块依赖图   |
| GET  | `/api/modules/verify` | 模块边界验证 |

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
- 对外错误码与统一响应约定见：[docs/api-error-codes.md](docs/api-error-codes.md)
- 开发规范与命名约定见：[docs/development-standards.md](docs/development-standards.md)

## 运行测试
```bash
dotnet test
dotnet test tests/DotNetModulith.Modules.Orders.Tests
dotnet test tests/DotNetModulith.ArchitectureTests
dotnet test tests/DotNetModulith.IntegrationTests
```

## 许可证
[MIT](https://opensource.org/licenses/MIT)
