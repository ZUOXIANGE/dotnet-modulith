# 认证鉴权与 RBAC 说明

本文档基于当前代码实现整理项目中的用户认证、接口鉴权与 RBAC 落地方式，并说明如何把同一套权限模型扩展到 `Orders`、`Inventory`、`Payments`、`Modules` 等模块。

## 1. 当前实现概览

项目当前已经实现了一套完整的基于 JWT 的认证与基于权限声明的授权链路，核心代码集中在：

- `src/DotNetModulith.Api/Program.cs`
- `src/DotNetModulith.Modules.Users/UsersServiceCollectionExtensions.cs`
- `src/DotNetModulith.Modules.Users/Application/UsersModuleAuth.cs`
- `src/DotNetModulith.Modules.Users/Application/UserIdentityService.cs`
- `src/DotNetModulith.Modules.Users/Domain/UserManagement.cs`
- `src/DotNetModulith.Modules.Users/Infrastructure/UsersDbContext.cs`

当前状态可以概括为：

- 已实现 `JWT Bearer Authentication`
- 已实现 `User -> Role -> Permission` 的 RBAC 数据模型
- 已实现 `会话级失效` 与 `TokenVersion` 双重令牌失效控制
- 已实现按 `Permission` 动态注册授权策略
- 已在 `Users`、`Orders`、`Inventory`、`Modules` 控制器上启用权限校验
- `Payments`、`Notifications`、`JobHost` 当前还没有对外受保护 API，因此暂未纳入 RBAC 控制面

## 2. 认证流程是怎么实现的

### 2.1 服务注册入口

API 启动时在 `Program.cs` 中注册认证鉴权：

- `builder.Services.AddUsersAuthentication(builder.Configuration);`
- `app.UseAuthentication();`
- `app.UseAuthorization();`

其中 `AddUsersAuthentication()` 位于 `UsersServiceCollectionExtensions.cs`，负责：

- 读取 `Authentication:Jwt` 配置
- 配置 `JwtBearer` 认证方案
- 关闭 `RequireHttpsMetadata`
- 设置 `Issuer`、`Audience`、`SecretKey`、过期校验
- 遍历所有权限点并动态注册同名授权策略

### 2.2 JWT 配置来源

JWT 配置定义在：

- `src/DotNetModulith.Modules.Users/Application/UsersModuleAuth.cs`

配置节点在：

- `src/DotNetModulith.Api/appsettings.json`

配置结构如下：

```json
"Authentication": {
  "Jwt": {
    "Issuer": "dotnet-modulith",
    "Audience": "dotnet-modulith-api",
    "SecretKey": "DotNetModulith-Development-Key-2026",
    "AccessTokenLifetimeMinutes": 120
  }
}
```

### 2.3 登录流程

登录入口是 `POST /api/auth/login`，由 `AuthController.Login()` 调用 `IUserIdentityService.LoginAsync()` 完成。

登录时的主要步骤：

1. 根据用户名查询 `users.users`
2. 校验用户是否存在
3. 校验用户是否启用
4. 使用 `PasswordHasher<ModuleUser>` 校验密码哈希
5. 生成新的 `sessionId`
6. 调用 `JwtTokenFactory.CreateAccessToken()` 签发访问令牌
7. 记录 `LastLoginAt`
8. 向 `users.user_sessions` 写入一条会话记录
9. 返回 `accessToken`、过期时间和当前用户信息

这意味着当前实现不是“只认 JWT 签名”的纯无状态方案，而是“JWT + 服务端会话表”的混合方案。

## 3. JWT 中放了什么声明

`JwtTokenFactory` 在签发令牌时写入了以下声明：

- `sub`: 用户 Id
- `nameidentifier`: 用户 Id
- `unique_name`: 用户名
- `name`: 显示名
- `email`: 邮箱
- `jti`: 当前会话 Id
- `modulith_session_id`: 当前会话 Id
- `modulith_token_version`: 用户当前令牌版本

需要注意的一点：

- 初次签发 JWT 时，并没有把角色和权限直接写进 Token
- 角色和权限是在 `OnTokenValidated` 阶段，通过数据库重新查询后补进 `ClaimsPrincipal`

这样做的好处是：

- 权限变更不依赖等待旧 Token 自然过期
- 可通过数据库中的角色和权限关系实时重建当前授权上下文
- 能与会话表、TokenVersion 一起形成更可靠的失效机制

## 4. 请求进入后的鉴权流程

### 4.1 JWT 基础校验

`JwtBearer` 先完成标准校验：

- 签名是否合法
- `Issuer` 是否匹配
- `Audience` 是否匹配
- 是否过期

### 4.2 自定义会话校验

标准 JWT 校验通过后，`JwtBearerEventsFactory.Create()` 中的 `OnTokenValidated` 会继续调用 `IJwtSessionValidator.ValidateAsync()`。

这里会再次校验：

- 用户 Id 是否存在
- `sessionId` 是否存在
- `tokenVersion` 是否可解析
- 用户是否仍然存在
- 用户是否为启用状态
- 数据库中的 `TokenVersion` 是否和 Token 声明一致
- `users.user_sessions` 中是否仍然存在该会话
- 会话是否已被撤销

只要其中任意一步失败，请求就会被判定为未授权。

### 4.3 权限声明注入

当会话有效时，系统会重新构造 `ClaimsPrincipal`，附加：

- `ClaimTypes.Role`: 用户拥有的角色名
- `modulith_permission`: 用户通过角色聚合得到的权限码

因此后续控制器上的 `[Authorize]` 和 `[Authorize(Policy = "...")]` 实际依赖的是“数据库实时重建后的 Claims”，而不是登录时写死在 JWT 中的角色权限。

## 5. 令牌为什么可以被强制失效

当前实现同时使用了两套机制。

### 5.1 会话表撤销

`users.user_sessions` 记录每次登录签发的 `TokenId`、签发时间、过期时间、撤销时间和撤销原因。

下列操作会直接撤销会话：

- 当前用户主动登出 `LogoutAsync`
- 管理员强制登出他人 `ForceLogoutAsync`
- 修改密码 `ChangeCurrentPasswordAsync`
- 重置密码 `ResetPasswordAsync`

### 5.2 TokenVersion 失效

`ModuleUser.TokenVersion` 会参与 Token 声明，并在校验时与数据库做比对。

下列操作会提升 `TokenVersion`：

- 设置密码 `SetPassword`
- 强制登出 `ForceLogoutAsync`
- 禁用用户 `SetUserStatusAsync(false, ...)`

这样即使某些旧会话记录未被及时消费，只要 `TokenVersion` 不匹配，旧 Token 仍会整体失效。

## 6. RBAC 模型是怎么建的

当前 RBAC 采用标准的三层关系：

- 用户 `ModuleUser`
- 角色 `Role`
- 权限 `Permission`

关系表为：

- `users.user_roles`: 用户和角色多对多
- `users.role_permissions`: 角色和权限多对多

配套表还有：

- `users.users`
- `users.roles`
- `users.user_sessions`

数据库映射位于：

- `src/DotNetModulith.Modules.Users/Infrastructure/UsersDbContext.cs`

当前权限目录定义在 `UserPermissions`：

- `users.view`
- `users.manage`
- `roles.view`
- `roles.manage`
- `orders.view`
- `orders.manage`
- `inventory.view`
- `inventory.manage`
- `modules.view`

系统启动时会自动种子一个系统角色和管理员账号：

- 角色：`Admin`
- 用户名：`admin`
- 初始密码：`Admin@123456`

种子逻辑会把 `Admin` 角色赋予 `UserPermissions.All` 中的全部权限。

## 7. 授权策略是怎么和权限点绑定的

在 `AddUsersAuthentication()` 中，系统会遍历 `UserPermissions.All`，为每个权限点注册一个同名策略。

例如：

- 权限码 `users.view`
- 对应策略名也是 `users.view`

每个策略包含两层要求：

- `RequireAuthenticatedUser()`
- `PermissionRequirement(permission)`

真正的权限判断由 `PermissionAuthorizationHandler` 完成，它只做一件事：

- 检查当前 `ClaimsPrincipal` 是否包含 `modulith_permission = 指定权限码`

所以这里的核心思想是：

- 策略名就是权限码
- 控制器声明策略
- 授权处理器检查权限声明

## 8. 当前哪些接口已经接入 RBAC

### 8.1 AuthController

`AuthController` 采用两类保护方式：

- `POST /api/auth/login` 使用 `[AllowAnonymous]`
- `POST /api/auth/logout` 使用 `[Authorize]`
- `GET /api/auth/me` 使用 `[Authorize]`
- `POST /api/auth/change-password` 使用 `[Authorize]`

这类接口只要求“已登录”，不要求具体业务权限。

### 8.2 UsersController

`UsersController` 已完整接入权限策略：

- 查询用户列表: `users.view`
- 查询用户详情: `users.view`
- 创建用户: `users.manage`
- 编辑用户: `users.manage`
- 分配角色: `users.manage`
- 设置用户状态: `users.manage`
- 强制登出: `users.manage`
- 重置密码: `users.manage`

### 8.3 RolesController

`RolesController` 也已完整接入权限策略：

- 查询角色列表: `roles.view`
- 查询权限目录: `roles.view`
- 创建角色: `roles.manage`
- 更新角色权限: `roles.manage`

### 8.4 OrdersController

`OrdersController` 已接入权限策略：

- 创建订单: `orders.manage`
- 确认订单: `orders.manage`
- 查询订单详情: `orders.view`
- 手动清理订单缓存: `orders.manage`

### 8.5 InventoryController

`InventoryController` 已接入权限策略：

- 查询库存: `inventory.view`
- 创建库存: `inventory.manage`
- 补充库存: `inventory.manage`

### 8.6 ModulesController

`ModulesController` 已接入权限策略：

- 查询模块列表: `modules.view`
- 查询依赖图: `modules.view`
- 执行边界校验: `modules.view`

## 9. 当前哪些模块还没有接入 RBAC

当前对外控制器里，`Users`、`Orders`、`Inventory`、`Modules` 都已经接入了 RBAC。

目前仍未纳入 RBAC 讨论范围的模块主要是：

- `Payments`
- `Notifications`
- `JobHost`

原因不是“漏做了鉴权”，而是这些模块当前没有面向外部的业务 Controller，主要通过领域事件、集成事件或后台作业运行。

## 10. 如何把 RBAC 应用到各个模块

推荐沿用当前项目已经实现的模式，不要为每个模块再造一套授权体系。

### 10.1 第一步：先定义模块权限点

当前项目已经采用统一权限目录，后续新增模块时建议继续沿用，例如：

```csharp
public static class UserPermissions
{
    public const string OrdersView = "orders.view";
    public const string OrdersManage = "orders.manage";
    public const string InventoryView = "inventory.view";
    public const string InventoryManage = "inventory.manage";
    public const string ModulesView = "modules.view";
}
```

把这些权限加入 `Definitions` 和 `All` 后，系统启动时就会自动为新权限注册策略。

### 10.2 第二步：按模块职责拆分只读和写入权限

建议最少按“查看”和“管理”拆成两档：

- `orders.view`: 查询订单详情、查询订单列表
- `orders.manage`: 创建订单、确认订单、清缓存
- `inventory.view`: 查询库存
- `inventory.manage`: 创建库存、补充库存
- `modules.view`: 查看模块列表、依赖图、边界校验结果

这样做的好处：

- 读写边界清晰
- 更容易给运营、客服、仓储、管理员分配不同角色
- 不会把所有接口都粗暴归到一个超大权限里

### 10.3 第三步：在控制器上声明策略

例如订单模块可以这样接入：

```csharp
[Authorize(Policy = UserPermissions.OrdersManage)]
[HttpPost]
public async Task<ApiResponse<CreateOrderResponse>> CreateOrder(...)
```

```csharp
[Authorize(Policy = UserPermissions.OrdersView)]
[HttpGet("{orderId:guid}")]
public async Task<ApiResponse<OrderDetail>> GetOrder(...)
```

库存模块同理：

```csharp
[Authorize(Policy = UserPermissions.InventoryView)]
[HttpGet("stocks/{productId}")]
public async Task<ApiResponse<StockDetail>> GetStock(...)
```

```csharp
[Authorize(Policy = UserPermissions.InventoryManage)]
[HttpPost("stocks/{productId}/replenish")]
public async Task<ApiResponse<object?>> ReplenishStock(...)
```

### 10.4 第四步：通过角色聚合模块权限

有了模块权限点后，再定义角色组合。例如：

- `Admin`: 全部权限
- `UserAdmin`: `users.*` + `roles.*`
- `OrderOperator`: `orders.view` + `orders.manage`
- `InventoryOperator`: `inventory.view` + `inventory.manage`
- `Auditor`: 仅各模块 `*.view`

当前项目的角色管理接口已经支持：

- 创建角色
- 更新角色权限
- 把角色分配给用户

因此扩展模块权限后，现有角色管理能力可以直接复用，无需重写鉴权基础设施。

### 10.5 第五步：为新模块补充集成测试

建议新增测试覆盖以下场景：

- 未登录访问模块接口返回 `Unauthorized`
- 已登录但缺少权限返回 `Forbidden`
- 拥有权限时访问成功
- 修改角色权限后重新请求生效
- 强制登出、禁用用户、重置密码后旧 Token 失效

## 11. 推荐的模块权限划分

下面给出一份适合当前项目结构的建议清单。

| 模块 | 建议权限 | 适用接口 |
| ---- | ---- | ---- |
| Users | `users.view` `users.manage` | 用户查询、创建、编辑、状态变更、重置密码 |
| Roles | `roles.view` `roles.manage` | 角色查询、权限目录查询、角色创建、权限维护 |
| Orders | `orders.view` `orders.manage` | 创建订单、确认订单、查询订单、清缓存 |
| Inventory | `inventory.view` `inventory.manage` | 查询库存、创建库存、补货 |
| Payments | `payments.view` `payments.manage` | 未来支付查询、补单、对账、人工确认 |
| Modules | `modules.view` | 模块列表、依赖图、边界校验 |

如果后续需要更细粒度控制，可以再拆分成：

- `orders.create`
- `orders.confirm`
- `inventory.replenish`
- `inventory.create`
- `modules.verify`

但在当前项目阶段，建议先采用“view/manage”两级模型，复杂度更可控。

## 12. 当前实现的优点

- 不是简单的前端保存 JWT，而是有服务端会话撤销能力
- 角色和权限在请求时实时从数据库装载，权限变更更容易生效
- 通过 `TokenVersion` 避免密码修改、账号禁用后的旧 Token 继续可用
- 策略名直接复用权限码，控制器使用简单
- 角色、权限、用户管理能力已经具备，扩展到其他模块的成本较低

## 13. 当前实现的限制

当前仍存在几个明显边界：

- 登录签发时未把角色权限直接放入 JWT，需要每次请求访问数据库
- 当前权限目录集中在 `Users` 模块领域对象中，后续模块权限增多时需要考虑是否继续集中维护
- `Payments` 等尚未暴露对外 API 的模块，未来一旦新增控制器，还需要补充对应权限点与策略

## 14. 推荐的实施顺序

如果要把 RBAC 真正覆盖整个项目，建议按以下顺序推进：

1. 为新增 API 模块先补齐权限常量
2. 给新控制器逐个加上 `[Authorize(Policy = ...)]`
3. 为不同业务岗位创建角色并配置权限
4. 增加未授权、无权限、会话失效相关测试
5. 评估是否把权限目录按模块拆分文件组织

## 15. 一句话总结

当前项目已经具备一套可用的认证鉴权内核：

- 认证用 `JWT Bearer`
- 会话有效性靠 `user_sessions + token_version`
- 授权采用 `User -> Role -> Permission`
- 控制器通过 `[Authorize(Policy = 权限码)]` 落地

当前项目已经把 RBAC 落到了 `Users`、`Orders`、`Inventory`、`Modules` 这些对外 API 模块。后续如果新增 `Payments` 等外部接口，最直接的方式仍然是继续扩展 `UserPermissions`，并在控制器上显式声明对应权限策略。
