---
name: "dotnet-modulith-object-mapping-zh"
description: "规范 DotNetModulith 项目的对象映射、DTO 放置与 Mapperly 使用方式。用户修改映射、DTO、Mapper、Contracts 或做结构整理时调用。"
---

# DotNetModulith 对象映射规范技能

用于在本仓库中统一对象映射的目录结构、职责边界与 `Mapperly` 使用方式，避免 DTO、契约、Mapper、事件映射互相混放

## 何时调用

- 用户要求新增或修改对象映射
- 用户要求引入或调整 `Mapperly`
- 用户修改 `Api/Mappings`、`Application/Mappings`、`Contracts`、`Queries`、`Models`
- 用户要求清理“DTO 放错目录”“映射写在契约文件里”“Mapper 文件里夹带 DTO”
- 用户要求做模块内部结构规范化或一致性重构

## 必须遵循的项目约束

1. 映射器放置规则
- 接口层映射器放在 `Api/Mappings`
- 应用层映射器放在 `Application/Mappings`
- 不在 `Contracts/Requests`、`Contracts/Responses`、`Api/Contracts` 中放手写映射逻辑
- 不在 Controller 中内联对象映射实现

2. DTO 与契约放置规则
- API 请求/响应模型只放在 `Api/Contracts/Requests`、`Api/Contracts/Responses`
- 查询结果 DTO 优先放在对应查询目录，例如 `Application/Queries/<Feature>`
- 通用应用层 DTO 放在明确的 `Application/Models` 或对应功能目录
- 不把 DTO/record 定义放在 `*Mapper.cs` 文件底部

3. Mapperly 使用规则
- 优先使用 `Riok.Mapperly` 生成纯对象映射代码
- 映射器类命名使用 `*Mapper`
- 映射器使用 `[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]`
- 能由 `Mapperly` 直接表达的字段映射，不再手写 `ToResponse()`、`ToDto()`、`ToCommand()` 扩展
- 仅在存在值对象转换、枚举字符串转换、字段重命名时，补充必要的 `MapProperty` 或私有辅助转换方法

4. 手写映射的边界
- 领域事件到集成事件的映射允许手写
- 包含业务判断、条件分支、副作用的逻辑不放进 `Mapperly`
- 手写事件映射优先放到独立文件或明确的事件映射区域，不和 DTO 定义混放

5. 命名与职责
- `ToResponse()` 用于应用层结果到 API 响应
- `ToCommand()` 用于请求到命令
- `ToDetail()` / `ToContract()` 用于领域对象到 DTO/契约
- 一个映射器文件只承载“映射器本身”，不要同时承载 DTO 定义、配置常量、业务逻辑
- 数据库实体类型统一使用 `*Entity` 后缀；映射目标若为实体需明确使用 `XxxEntity`

## 本仓库的推荐目录模式

1. API 响应映射
- `src/<Module>/Api/Contracts/Responses/*.cs`
- `src/<Module>/Api/Mappings/*ResponseMapper.cs`

2. API 请求映射
- `src/<Module>/Api/Contracts/Requests/*.cs`
- `src/<Module>/Api/Mappings/*RequestMapper.cs`

3. 查询 DTO 映射
- `src/<Module>/Application/Queries/<Feature>/*.cs`
- `src/<Module>/Application/Mappings/*Mapper.cs`

4. 通用应用层模型
- `src/<Module>/Application/Models/*.cs`
- 仅当该 DTO 不隶属于单个 Query/Command 场景时使用

## 实施步骤模板

1. 先读取并对齐以下位置
- 目标模块的 `Api/Contracts`
- 目标模块的 `Api/Mappings`
- 目标模块的 `Application/Mappings`
- 目标模块的 `Application/Queries`
- 目标模块的 `.csproj`
- `Directory.Packages.props`

2. 改动时强制检查
- 映射逻辑是否仍然混在 `Contracts` 文件中
- DTO 是否错误地定义在 `*Mapper.cs` 文件中
- 是否已经有现成 `Mapperly` 可复用而不是继续手写
- `Mapperly` 需要的 `PackageReference` 是否已存在
- 命名空间是否随文件移动同步更新

3. 改动优先级
- 先拆分“文件位置错误”的 DTO 和映射器
- 再替换“纯字段复制”的手写映射为 `Mapperly`
- 最后考虑是否拆分事件映射、值对象转换等特殊逻辑

4. 改动后验证
- `dotnet format`
- `dotnet build`
- 与目标模块相关的测试项目
- 若改动 API 返回模型，还要验证对应集成测试

## 常见反模式

- 在 `Api/Contracts/Responses` 文件底部写 `static class *Mappings`
- 在 `*Mapper.cs` 中同时定义 DTO record
- Controller 里手写 `new Response(...)` 串联一大段字段复制
- 为了少建文件，把查询 DTO、响应 DTO、映射器、事件映射全部塞进一个文件
- 明明是纯对象映射，却不使用 `Mapperly`

## 推荐整改策略

- `Contracts` 中发现映射方法：迁到 `Api/Mappings`
- `Mapper` 中发现 DTO 定义：移动到对应 `Queries` 或 `Application/Models`
- 事件映射与 DTO 映射混在一起：按职责拆到独立文件
- 纯映射扩展类：改为 `Mapperly` partial mapper

## 输出风格

- 优先给“是否存在结构问题 + 清单 + 建议整改顺序”
- 若实际动手修改，明确“移动了哪些 DTO、替换了哪些手写映射、保留了哪些手写事件映射”
- 提供可点击文件链接，便于快速审阅
