# API 错误码对照（对外）

本文档面向前端与外部调用方，约定 `DotNetModulith` 的统一响应与错误码语义。

## 响应格式

所有接口（成功/失败）均返回 `HTTP 200`，通过 `code` 区分业务结果。

```json
{
  "msg": "success",
  "code": 200,
  "data": {}
}
```

- `msg`：结果描述
- `code`：业务码（见下文）
- `data`：业务数据或错误详情

## 通用错误码（ApiCodes.Common）

| code | 常量                               | 含义             | 建议处理                   |
| ---- | ---------------------------------- | ---------------- | -------------------------- |
| 200  | `ApiCodes.Common.Success`          | 成功             | 正常渲染数据               |
| 400  | `ApiCodes.Common.ValidationFailed` | 请求参数校验失败 | 提示用户修正输入           |
| 401  | `ApiCodes.Common.Unauthorized`     | 未登录或令牌失效 | 触发重新登录               |
| 403  | `ApiCodes.Common.Forbidden`        | 无权限访问       | 提示无权限并隐藏相关操作   |
| 404  | `ApiCodes.Common.NotFound`         | 资源不存在       | 提示“数据不存在”并允许重试 |
| 500  | `ApiCodes.Common.InternalError`    | 服务内部错误     | 提示稍后重试并上报日志     |

## 认证域错误码（ApiCodes.Auth）

| code  | 常量                               | 含义             | 触发场景                    |
| ----- | ---------------------------------- | ---------------- | --------------------------- |
| 40001 | `ApiCodes.Auth.InvalidCredentials` | 用户名或密码错误 | 登录失败                    |
| 40002 | `ApiCodes.Auth.InvalidToken`       | 令牌无效或已失效 | 登出后令牌继续访问、黑名单  |
| 40003 | `ApiCodes.Auth.UserDisabled`       | 用户已被禁用     | 被停用账号继续尝试登录      |

## 订单域错误码（ApiCodes.Order）

| code  | 常量                          | 含义                   | 触发场景             |
| ----- | ----------------------------- | ---------------------- | -------------------- |
| 10001 | `ApiCodes.Order.InvalidState` | 订单状态不允许当前操作 | 如已取消订单再次确认 |

## 库存域错误码（ApiCodes.Inventory）

| code  | 常量                                   | 含义     | 触发场景         |
| ----- | -------------------------------------- | -------- | ---------------- |
| 20001 | `ApiCodes.Inventory.InsufficientStock` | 库存不足 | 下单预占库存不足 |

## 支付域错误码（ApiCodes.Payment）

| code  | 常量                                | 含义         | 触发场景               |
| ----- | ----------------------------------- | ------------ | ---------------------- |
| 30001 | `ApiCodes.Payment.ProcessingFailed` | 支付处理失败 | 支付通道异常或业务拒绝 |

## 用户与角色域错误码（ApiCodes.User）

| code  | 常量                                | 含义         | 触发场景                 |
| ----- | ----------------------------------- | ------------ | ------------------------ |
| 50001 | `ApiCodes.User.UserNameAlreadyExists` | 用户名已存在 | 创建用户时用户名重复     |
| 50002 | `ApiCodes.User.EmailAlreadyExists`    | 邮箱已存在   | 创建用户时邮箱重复       |
| 50003 | `ApiCodes.User.RoleNotFound`          | 角色不存在   | 分配角色时角色标识无效   |
| 50004 | `ApiCodes.User.InvalidPermission`     | 权限编码无效 | 创建或更新角色权限时非法 |
| 50005 | `ApiCodes.User.RoleNameAlreadyExists` | 角色名已存在 | 创建角色时名称重复       |
| 50006 | `ApiCodes.User.InvalidCurrentPassword` | 当前密码错误 | 用户修改本人密码时校验失败 |

## 兼容性约定

- 现有 `code` 不做语义变更。
- 新增错误码只增不减，避免破坏客户端兼容性。
- 推荐客户端按“`code` + `msg`”双维度处理，避免仅依赖文案。
