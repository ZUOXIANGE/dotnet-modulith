# 定时任务开发说明

本文档说明如何在当前 `DotNetModulith` 项目中编写、注册、验证一个基于 `TickerQ` 的定时任务。

本项目的定时任务实现不是通过 `BackgroundService` 手工轮询，而是统一使用：

- `TickerQ` 负责任务调度与执行
- `PostgreSQL` 中独立的 `tickerq` 数据库存储任务定义与执行记录
- `CAP` 负责跨模块集成事件发布
- `OpenTelemetry` 负责任务执行链路和指标观测

## 1. 当前实现概览

项目中已有一个完整示例：库存低库存扫描任务。

关键代码位置：

- 任务实现：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`
- 任务配置：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertOptions.cs`
- `TickerQ` 宿主接入：`../src/DotNetModulith.JobHost/Program.cs`
- `TickerQ` 调度库上下文：`../src/DotNetModulith.JobHost/Infrastructure/TickerQSchedulerDbContext.cs`

当前这条链路的职责分工是：

- `Job` 只负责“扫描 + 决策 + 发布事件”
- 模块内部状态更新和事件发布放在同一个事务中
- 后续通知、联动等逻辑交给其他模块通过事件订阅处理

## 2. 什么时候适合写定时任务

适合用定时任务的场景：

- 周期性扫描数据并触发业务动作
- 定时补偿、超时关闭、过期清理
- 周期性汇总、对账、刷新缓存
- 需要持久化执行记录和失败重试的后台任务

不建议使用定时任务的场景：

- 一个 HTTP 请求内就应该同步完成的逻辑
- 可以由领域事件立即驱动的逻辑
- 需要高频短周期、且更适合消息队列消费的任务

## 3. 项目中的定时任务架构

当前项目采用“独立 `JobHost` 统一调度，业务模块提供任务实现”的方式。

整体流程：

1. 在业务模块中新增 `Job` 类
2. 用 `[TickerFunction(...)]` 声明函数名和 Cron 表达式
3. 通过构造函数注入模块仓储、`DbContext`、配置、日志等依赖
4. 在任务内执行业务扫描
5. 如需跨模块通知，通过 `CAP` 发布集成事件
6. `TickerQ` 将任务定义和执行记录写入独立调度库
7. 通过 `JobHost` 的 `/tickerq-dashboard` 查看执行情况

## 4. 目录放在哪里

推荐放在业务模块的 `Application/Jobs` 目录下。

例如当前库存模块：

```text
src/DotNetModulith.Modules.Inventory/
  Application/
    Jobs/
      LowStockAlertJob.cs
      LowStockAlertOptions.cs
```

这样做的原因：

- 任务属于应用层用例，不属于 `Api` 或 `JobHost` 的入口层
- 任务通常会编排仓储、事件发布、状态更新
- 配置项和任务实现放在一起更容易维护

## 5. 如何编写一个 Job

### 5.1 创建任务类

任务类使用普通 `class` 即可，不需要继承基类。

建议模式：

- 使用显式构造函数注入依赖
- 保持单一职责
- 方法签名使用 `Task`
- 最后一个参数带 `CancellationToken`

参考当前实现：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`

示例骨架：

```csharp
using Microsoft.Extensions.Logging;
using TickerQ.Utilities.Base;

namespace DotNetModulith.Modules.Example.Application.Jobs;

public sealed class ExampleJob
{
    private readonly ILogger<ExampleJob> _logger;

    public ExampleJob(ILogger<ExampleJob> logger)
    {
        _logger = logger;
    }

    [TickerFunction("Example.Run", cronExpression: "0 */10 * * * *")]
    public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Example job started. TickerId: {TickerId}", context.Id);

        await Task.CompletedTask;
    }
}
```

### 5.2 使用 `[TickerFunction]` 声明调度信息

当前项目通过特性声明任务函数名与 Cron。

库存示例：

```csharp
[TickerFunction("Inventory.LowStockAlertScan", cronExpression: "*/5 * * * *")]
public async Task ExecuteAsync(TickerFunctionContext context, CancellationToken cancellationToken)
```

建议：

- `Function` 名称使用 `模块名.任务名`
- 名称要稳定，避免频繁改名
- Cron 规则优先配置化；如果暂时写死，至少在文档里说明

命名建议：

- `Inventory.LowStockAlertScan`
- `Orders.TimeoutClose`
- `Payments.Reconcile`
- `Notifications.Cleanup`

## 6. 如何注入依赖

任务类和普通应用服务一样，通过 DI 解析。

当前库存任务注入了：

- `IStockRepository`
- `InventoryDbContext`
- `ICapPublisher`
- `IOptions<LowStockAlertOptions>`
- `ILogger<LowStockAlertJob>`

参考：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`

建议优先注入：

- 模块仓储接口
- 当前模块 `DbContext`
- `IOptions<T>` 配置
- `ILogger<T>`
- 必要的事件发布器或外部客户端

不建议：

- 在任务里直接 `new` 依赖
- 在任务里堆积过多跨模块调用
- 在任务里写大量 Controller 风格的参数校验逻辑

## 7. 如何做配置

任务相关配置应放在独立 `Options` 类中，并使用 `DataAnnotations` 校验。

当前库存任务配置：

- `../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertOptions.cs`

示例：

```csharp
public sealed class ExampleJobOptions
{
    public const string SectionName = "ExampleJob";

    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;
}
```

建议：

- 每个任务或任务族单独一个配置节
- 使用 `ValidateDataAnnotations` 和 `ValidateOnStart`
- 配置项只存“阈值、批次、开关”等业务参数
- 连接串、调度库等基础设施配置仍由宿主统一管理

## 8. 如何处理数据库与事务

### 8.1 业务数据和调度数据分库

本项目中：

- 业务表在 `modulithdb`
- 调度表在 `tickerqdb`

`TickerQ` 独立调度库上下文见：

- `../src/DotNetModulith.JobHost/Infrastructure/TickerQSchedulerDbContext.cs`

宿主接入见：

- `../src/DotNetModulith.JobHost/Program.cs`

### 8.2 业务状态更新和事件发布要放在同一个事务中

当前库存任务使用：

- `CapTransactionScope.ExecuteAsync(...)`

这样可以保证：

- 低库存标记已落库
- 集成事件已写入 CAP
- 两者要么一起成功，要么一起失败

参考：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`

推荐模式：

```csharp
await CapTransactionScope.ExecuteAsync(
    dbContext,
    capPublisher,
    async ct =>
    {
        // 1. 更新业务实体状态
        // 2. 保存业务数据
        // 3. 发布集成事件
    },
    cancellationToken);
```

## 9. 如何发布跨模块事件

如果任务执行结果需要通知其他模块，不要直接从任务里调用其他模块内部服务，优先发布集成事件。

库存示例发布的是：

- `modulith.inventory.LowStockDetectedIntegrationEvent`

这样做的好处：

- 任务和下游模块解耦
- 失败重试交给消息系统
- 可以自然扩展多个订阅者

建议：

- 任务只负责“发现事实”
- 后续动作通过事件订阅者处理

## 10. 如何做可观测性

本项目中的任务实现已经接入：

- `ActivitySource`
- `Meter`
- `Counter`
- 结构化日志

参考：`../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`

建议至少打这些信号：

- 任务开始/结束
- 匹配到的数据量
- 实际处理的数据量
- 失败异常
- 关键业务标签，例如阈值、批次、任务名

建议命名：

- Span 名：`Inventory.LowStockAlertScan`
- Metric 名：`modulith.inventory.low_stock.scan_count`
- Tag：`modulith.job_name`

## 11. 如何在宿主中启用 TickerQ

当前项目在独立 `JobHost` 宿主中统一接入 `TickerQ`：

- `../src/DotNetModulith.JobHost/Program.cs`

关键点：

- 从 `ConnectionStrings:tickerqdb` 获取调度库连接
- 使用 `TickerQSchedulerDbContext`
- 明确设置 schema 为 `ticker`
- 在 `JobHost` 中启用 Dashboard

当前 Dashboard 地址：

- `{jobhost-base-url}/tickerq-dashboard`

## 12. 新增一个定时任务的推荐步骤

### 步骤 1：定义配置

在模块中新增 `*Options`：

- 配置节名称
- 阈值/批次/开关
- `DataAnnotations`

### 步骤 2：实现 Job

在 `Application/Jobs` 中新增 `*Job.cs`：

- 用 `[TickerFunction]` 声明任务
- 注入需要的仓储、配置、日志
- 保持方法幂等

### 步骤 3：如需跨模块联动，定义集成事件

建议放在共享契约中，例如：

- `src/DotNetModulith.Abstractions/Contracts/...`

### 步骤 4：在订阅模块中新增事件处理器

不要让 Job 直接依赖下游模块内部实现。

### 步骤 5：补迁移或确认调度库迁移

如果涉及 TickerQ 表结构演进，更新：

- `TickerQSchedulerDbContext`
- 对应迁移文件

### 步骤 6：启动 Aspire 验证

验证项：

- `api` 资源 `Ready`
- `jobhost` 资源 `Ready`
- `/tickerq-dashboard` 可访问
- `CronTickers` 中出现对应任务
- `CronTickerOccurrences` 有执行记录
- 业务表状态符合预期
- 事件和通知链路正常

## 13. 如何验证任务是否真正工作

### 13.1 看 Dashboard

访问：

- `{jobhost-base-url}/tickerq-dashboard`

可以看到：

- 已注册任务
- 执行状态
- 执行历史

### 13.2 查调度库

可直接检查：

- `ticker."CronTickers"`
- `ticker."CronTickerOccurrences"`

例如：

```sql
select "Function", "Expression", "IsEnabled"
from ticker."CronTickers";

select count(*), max("ExecutionTime")
from ticker."CronTickerOccurrences";
```

### 13.3 查业务表

例如库存任务会更新：

- `inventory.stocks.low_stock_alert_sent_at`
- `inventory.stocks.last_alerted_available_quantity`

### 13.4 查应用日志

可以搜索：

- `TickerQ Job enqueued`
- `TickerQ Job completed`
- 任务类名
- 集成事件名

## 14. 编写定时任务时的约束与建议

### 建议遵循

- 任务逻辑保持幂等
- 使用配置而不是硬编码阈值
- 通过仓储和应用层编排，不直接写杂乱 SQL
- 发布事件而不是直接跨模块调用
- 始终写日志和指标
- 为关键任务补集成测试

### 尽量避免

- 一个任务里写太多跨模块业务
- 任务里直接发 HTTP 调用其他模块接口
- 不带事务地“先更新状态、再发消息”
- 不做去重，导致重复通知
- 将调度表混入业务数据库

## 15. 现成示例可以参考什么

建议直接参考以下文件：

- `../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertJob.cs`
- `../src/DotNetModulith.Modules.Inventory/Application/Jobs/LowStockAlertOptions.cs`
- `../src/DotNetModulith.Modules.Notifications/Application/Subscribers/NotificationEventSubscriber.cs`
- `../src/DotNetModulith.JobHost/Program.cs`
- `../src/DotNetModulith.JobHost/Infrastructure/TickerQSchedulerDbContext.cs`

## 16. 提交前建议检查

新增或修改定时任务后，建议至少执行：

```bash
dotnet format
dotnet build
dotnet test
```

如果任务涉及调度注册和真实依赖，建议额外验证：

- Aspire 下 `AppHost` 启动是否正常
- `TickerQ Dashboard` 是否可访问
- 任务是否写入 `CronTickers`
- 任务执行后业务状态是否符合预期
