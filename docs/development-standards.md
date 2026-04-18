# 项目开发规范（DotNetModulith）

本文档用于统一团队在本仓库中的编码、命名、接口、测试与文档实践。

## 1. 通用原则

- 使用 .NET 10 / C# 最新稳定语法，但以可读性和团队一致性优先。
- 默认遵循 `.editorconfig` 与 `dotnet format` 规则。
- 采用模块化单体（Modulith）分层：`Api`、`Application`、`Domain`、`Infrastructure`。
- 所有外部 HTTP 接口统一返回 `ApiResponse<T>`。

## 2. 目录与文件组织

- `src/DotNetModulith.Api`：宿主程序、全局中间件、通用 Controller（如模块管理）。
- `src/DotNetModulith.Modules.*`：业务模块实现。
- `src/DotNetModulith.Abstractions`：跨模块共享契约、结果模型、异常、校验辅助。
- `tests/*`：按模块与类型划分单元测试、架构测试、集成测试。

模块内推荐目录：

```text
Api/
  Controllers/
  Contracts/
    Requests/
    Responses/
Application/
  Commands/
  Queries/
  Mappings/
Domain/
Infrastructure/
```

## 3. 命名规范

- 命名空间：与目录结构一致，使用 PascalCase。
- 类/接口/枚举/记录：PascalCase。
- 方法/属性/事件：PascalCase。
- 局部变量与参数：camelCase。
- 私有字段：`_camelCase`。
- 接口前缀：`I`（如 `IOrderRepository`）。
- 异步方法后缀：`Async`（框架签名已有约定时可例外）。
- 请求模型：`*Request`；响应模型：`*Response`。
- 命令/查询：`*Command`、`*Query`；处理器：`*Handler`。
- 控制器：`*Controller`，按资源命名，避免语义不清。
- XML 注释（`summary`、`param`、`returns`）末尾不使用句号（中文 `。` 与英文 `.` 都禁止）。

## 4. Controller 与 API 规范

- 控制器必须添加 `[ApiController]`。
- 控制器路由使用 `[Route("api/...")]` + `[HttpGet]/[HttpPost]/...`。
- Action 返回类型使用强类型：`ApiResponse<T>` 或 `Task<ApiResponse<T>>`。
- 不在 Action 中返回 `ActionResult` / `IResult` 作为常规返回。
- 模型校验使用 ASP.NET Core 原生模型绑定与 DataAnnotations。
- 模型校验失败统一通过 `InvalidModelStateResponseFactory` 输出：
  - `HTTP 200`
  - `ApiResponse.Failure("validation failed", ApiCodes.Common.ValidationFailed, ...)`

## 5. 响应与错误码规范

- 响应结构固定：

```json
{
  "msg": "success",
  "code": 200,
  "data": {}
}
```

- 所有接口（成功/失败）统一返回 `HTTP 200`，业务结果依赖 `code`。
- 业务异常统一抛出 `BusinessException`，由全局异常处理中间件转换为 `ApiResponse`。
- 错误码仅在 `ApiCodes` 中定义并按分层管理：
  - `ApiCodes.Common`
  - `ApiCodes.Order`
  - `ApiCodes.Inventory`
  - `ApiCodes.Payment`
- 错误码对外说明维护在：`docs/api-error-codes.md`。

## 6. 校验规范（DataAnnotations）

- 禁止新增 FluentValidation 依赖与验证器。
- 请求模型字段校验使用 `DataAnnotations`：
  - `[Required]`、`[StringLength]`、`[Range]` 等。
- 可复用自定义校验特性放在：
  - `DotNetModulith.Abstractions/Validation/Attributes`
- 请求模型仅放在 `Api/Contracts/Requests`，避免散落在 Controller 文件底部。

## 7. 对象映射规范

- 对象映射统一在 `Application/Mappings` 层实现，禁止在 Controller 中手写字段拷贝。
- 优先使用 Mapperly（`Riok.Mapperly`）生成映射代码，避免运行时反射映射。
- 映射类型命名使用 `*Mapper` 或 `*MappingExtensions`，并与业务模块同目录维护。
- 映射方法命名使用 `ToCommand`、`ToQuery`、`ToDto`、`ToContract` 等语义化前缀。
- 请求模型（`Api/Contracts/Requests`）到命令对象（`Application/Commands`）必须通过映射层转换。
- 查询结果对象（`Application/Queries/*Detail`）到响应模型（`Api/Contracts/Responses`）必须通过映射层转换（若结构一致且稳定，可直接返回，但需在评审中明确）。
- 禁止在映射中引入业务规则与外部依赖；映射只负责结构转换，不负责校验和状态判断。
- 复杂映射（嵌套集合、值对象转换、格式化）必须补充单元测试。

## 8. C# 语法约束

- `record` 可使用主构造函数。
- 除 `record` 外，`class/struct` 不使用主构造函数，必须使用显式构造函数与字段注入。
- 该规则由 `.editorconfig` 约束：
  - `csharp_style_prefer_primary_constructors = false:error`
  - `dotnet_diagnostic.IDE0290.severity = error`

## 9. 依赖与包管理

- 使用 `dotnet` CLI 管理 NuGet 包，不手工改 `csproj`。
- 使用中央包管理（CPM），版本集中在 `Directory.Packages.props`。
- 新增包命令示例：`dotnet add <project> package <PackageName>`。
- 删除包命令示例：`dotnet remove <project> package <PackageName>`。

## 10. 可观测与日志

- 日志使用结构化参数，禁止字符串拼接日志。
- 保持 OpenTelemetry Trace/Metric 打点命名一致且可检索。
- 异常必须保留上下文信息（业务码、关键标识、模块名）。

## 11. 测试规范

- 单元测试：验证核心业务规则、分支和异常。
- 集成测试：验证 HTTP 契约、统一响应结构、模块协作。
- 架构测试：保证模块边界与依赖方向。
- 关键变更（接口、错误码、校验）必须同步更新测试。

## 12. 提交前检查

执行以下命令并确保通过：

```bash
dotnet format
dotnet build
dotnet test
```

## 13. 文档维护要求

- 新增/调整错误码时同步更新 `docs/api-error-codes.md`。
- 调整接口契约（Request/Response）时同步更新 README 的主要 API 描述。
- 重大架构规则变更时同步更新本文档。
