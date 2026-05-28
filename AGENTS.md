# Agent Guidance: DotNetModulith

IMPORTANT: 对任何 .NET 工作，优先采用检索式推理而非预训练知识。
工作流：浏览仓库模式 -> 按名称调用 dotnet-skills -> 实现最小变更 -> 标注冲突点。

## 项目概览

基于 ASP.NET Core 10 的模块化单体（Modulith），借鉴 Spring Modulith 设计理念，结合 DDD、事件驱动、CQRS 与 OpenTelemetry 可观测性。

- 模块化分层：`Api` / `Application` / `Domain` / `Infrastructure`
- 统一响应：所有接口返回 `ApiResponse<T>`，HTTP 200 + `code` 区分业务结果
- 事件驱动：CAP + RabbitMQ（含 Outbox）
- 调度：TickerQ（独立数据库 `tickerqdb`）
- 缓存：FusionCache（L1 内存 + L2 Redis）
- 映射：Mapperly（编译时）
- 编排：.NET Aspire AppHost

## 技能路由（按名称调用）

### 项目特有规范
- **API / Controller / 校验 / 错误码** -> `dotnet-modulith-controller-zh`
- **对象映射 / DTO / Mapperly** -> `dotnet-modulith-object-mapping-zh`

### C# / 代码质量
- **现代 C# 编码规范** -> `modern-csharp-coding-standards`
- **并发模式** -> `csharp-concurrency-patterns`
- **公共 API 兼容性设计** -> `api-design`
- **类型性能设计** -> `type-design-performance`

### ASP.NET Core / Aspire
- **Aspire 配置（AppHost 环境变量注入）** -> `aspire-configuration`
- **Aspire 集成测试** -> `aspire-integration-testing`
- **ServiceDefaults（OTel/健康检查/弹性）** -> `aspire-service-defaults`
- **邮件测试（Mailpit）** -> `mailpit-integration`

### 数据访问
- **EF Core 最佳实践** -> `efcore-patterns`
- **数据库性能** -> `database-performance`

### DI / 配置
- **依赖注入组织** -> `dependency-injection-patterns`
- **Options 模式与配置验证** -> `microsoft-extensions-configuration`

### 测试
- **Testcontainers 集成测试** -> `testcontainers-integration-tests`
- **快照测试（Verify）** -> `snapshot-testing`
- **Blazor UI 测试（Playwright）** -> `playwright-blazor-testing`
- **Playwright CI 缓存** -> `playwright-ci-caching`

### 质量门禁（修改代码后建议执行）
- **LLM 代码反作弊检测** -> `dotnet-slopwatch`（每次 .cs/.csproj/.props 变更后）
- **CRAP 风险分析** -> `crap-analysis`（复杂代码新增/修改测试后）

### 构建与基础设施
- **项目结构与构建配置** -> `dotnet-project-structure`
- **NuGet 包管理（CPM）** -> `package-management`
- **本地工具管理** -> `dotnet-local-tools`
- **EditorConfig 规范** -> `dotnet-editorconfig`
- **HTTPS 开发证书修复** -> `dotnet-devcert-trust`

### 可观测性
- **OpenTelemetry 插桩** -> `OpenTelemetry-NET-Instrumentation`

### 序列化
- **序列化方案选择** -> `serialization`

### 其他
- **MJML 邮件模板** -> `mjml-email-templates`
- **邮件快照测试** -> `verify-email-snapshots`
- **ILSpy 反编译** -> `ilspy-decompile`

## 关键项目约定（速查）

| 领域      | 约定                                                         |
| --------- | ------------------------------------------------------------ |
| 响应格式  | `ApiResponse<T>`，统一 HTTP 200，`code` 区分业务结果         |
| 错误码    | `ApiCodes.Common/Order/Inventory/Payment` 分层常量           |
| 实体命名  | 数据库实体统一 `*Entity` 后缀                                |
| 请求/响应 | `*Request` / `*Response`                                     |
| 命令/查询 | `*Command` / `*Query`，处理器 `*Handler`                     |
| 控制器    | `[ApiController]` + `[Route("api/...")]`，返回强类型         |
| 模型校验  | DataAnnotations，失败统一 `ApiCodes.Common.ValidationFailed` |
| 业务异常  | `BusinessException`，全局中间件统一转换                      |
| 模块注册  | `IModule` 接口，`IServiceCollection.Add*()` 扩展方法         |
| 数据库    | 业务库 `modulithdb`，调度库 `tickerqdb`（独立）              |
| 映射器    | `Api/Mappings`（接口层）、`Application/Mappings`（应用层）   |
| XML 注释  | `summary/param/returns` 末尾不使用句号                       |
| 启动方式  | 统一 `dotnet run --project src/DotNetModulith.AppHost`       |

## 参考文档

- 认证鉴权与 RBAC：`docs/auth-rbac.md`
- 错误码约定：`docs/api-error-codes.md`
- 开发规范：`docs/development-standards.md`
- 定时任务：`docs/scheduled-jobs.md`
