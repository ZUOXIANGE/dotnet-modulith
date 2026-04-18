---
name: "dotnet-modulith-controller-zh"
description: "规范 DotNetModulith 项目的 Controller、ApiResponse、DataAnnotations、错误码与 XML/OpenAPI 实践。用户修改 API、校验、文档、README 时调用。"
---

# DotNetModulith 中文工程规范技能

用于在本仓库中实施统一的 API 设计与实现规范，确保代码、文档、测试、约定一致

## 何时调用

- 用户要求新增/修改 Controller 接口
- 用户调整请求/响应模型、参数校验、模型绑定行为
- 用户修改统一返回结构 `ApiResponse<T>` 或错误码 `ApiCodes`
- 用户要求补充 XML 注释、OpenAPI 展示、README/规范文档同步
- 用户要求做“项目规范化重构”或“代码风格对齐”

## 必须遵循的项目约束

1. 接口层
- Controller 必须使用 `[ApiController]`
- Action 返回强类型 `ApiResponse<T>` 或 `Task<ApiResponse<T>>`
- 不使用 `ActionResult` / `IResult` 作为常规返回

2. 响应协议
- 成功/失败统一 `HTTP 200`
- 通过 `ApiResponse.code` 区分业务结果
- 错误码必须来自 `ApiCodes` 分层常量（`Common/Order/Inventory/Payment`）

3. 校验与绑定
- 使用 ASP.NET Core 原生模型绑定 + DataAnnotations
- 不新增 FluentValidation
- 模型校验失败通过 `InvalidModelStateResponseFactory` 转换为统一响应

4. 注释与文档
- Controller、Action、Request/Response 模型必须有 XML 注释
- XML 注释末尾不允许句号（`。` 和 `.`）
- OpenAPI 文档应能展示 XML 注释内容
- README 的“接口、校验、响应约定”与实际实现保持一致

5. C# 语法
- 仅 `record` 允许主构造函数
- `class/struct` 禁止主构造函数（用显式构造函数 + 字段注入）

## 实施步骤模板

1. 先读取并对齐以下文件
- `src/DotNetModulith.Api/Program.cs`
- `src/DotNetModulith.Abstractions/Results/ApiResponse.cs`
- `src/DotNetModulith.Abstractions/Results/ApiCodes.cs`
- `docs/development-standards.md`
- `README.md`

2. 改动时强制检查
- 模型绑定来源是否明确（路由/查询/Body）
- DataAnnotations 是否覆盖关键字段
- `BusinessException` 是否携带正确业务码
- XML 注释是否完整且末尾无句号

3. 改动后验证
- `dotnet format`
- `dotnet build`
- `dotnet test tests/DotNetModulith.IntegrationTests`
- `dotnet test tests/DotNetModulith.Modules.Orders.Tests`

4. 文档同步
- 若影响契约：更新 `README.md`
- 若影响规范：更新 `docs/development-standards.md`
- 若影响错误码：更新 `docs/api-error-codes.md`

## 输出风格

- 优先给“结论 + 变更点 + 验证结果”
- 涉及规范变更时，明确“规则文本”与“实际代码落点”
- 提供可点击文件链接，便于快速审阅
