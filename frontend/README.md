# 图书馆管理系统 - 前端

基于 Vue 3 + TypeScript + Naive UI + Vite 构建的后台管理前端。

## 技术栈

| 类别      | 技术                    |
| --------- | ----------------------- |
| 框架      | Vue 3 (Composition API) |
| 语言      | TypeScript              |
| UI 组件库 | Naive UI                |
| 路由      | Vue Router 4            |
| 状态管理  | Pinia                   |
| 构建工具  | Vite                    |
| 图标      | @vicons/ionicons5       |

## 快速启动

### 前置条件

- Node.js 18+

### 安装依赖

```bash
cd frontend
npm install
```

### 启动开发服务器

```bash
npm run dev
```

默认运行在 `http://localhost:5173`，API 请求通过 Vite 代理转发到后端。

### 构建生产版本

```bash
npm run build
```

## 项目结构

```
frontend/
├── index.html
├── package.json
├── vite.config.ts
├── tsconfig.json
└── src/
    ├── main.ts                       # 入口
    ├── App.vue                       # 根组件
    ├── router/
    │   └── index.ts                  # 路由配置 + 导航守卫 + 权限校验
    ├── stores/
    │   └── auth.ts                   # 认证状态管理 (Pinia)
    ├── utils/
    │   └── api.ts                    # HTTP 请求封装 + 401 拦截
    ├── composables/
    │   └── usePermission.ts          # 前端权限判断
    ├── components/
    │   └── SelectorPopup.vue         # 通用选择器弹窗
    ├── layouts/
    │   └── MainLayout.vue            # 主布局（侧边栏 + 顶栏）
    ├── views/
    │   ├── login/                    # 登录页
    │   ├── dashboard/                # 工作台
    │   ├── books/                    # 图书管理 / 分类管理
    │   ├── members/                  # 读者管理
    │   ├── borrowing/                # 借还管理
    │   ├── reservations/             # 预约管理
    │   ├── fines/                    # 罚款管理
    │   ├── reports/                  # 统计报表
    │   ├── notifications/            # 消息通知
    │   └── users/                    # 用户管理 / 角色管理
    └── styles/
        └── global.css                # 全局样式
```

## 页面路由

| 路由             | 页面     | 说明                | 需要登录 | 需要权限             |
| ---------------- | -------- | ------------------- | -------- | -------------------- |
| `/login`         | 登录     | 管理员登录          | 否       | -                    |
| `/dashboard`     | 工作台   | 系统概览            | 是       | -                    |
| `/books`         | 图书管理 | 图书 CRUD           | 是       | `books.view`         |
| `/categories`    | 分类管理 | 多级分类树          | 是       | `categories.view`    |
| `/members`       | 读者管理 | 会员 CRUD           | 是       | `members.view`       |
| `/borrowing`     | 借还管理 | 借阅/归还/续借/丢失 | 是       | `borrowing.view`     |
| `/reservations`  | 预约管理 | 预约/取消           | 是       | `reservation.view`   |
| `/fines`         | 罚款管理 | 罚款/缴纳/豁免      | 是       | `fines.view`         |
| `/reports`       | 统计报表 | 数据统计            | 是       | `reports.view`       |
| `/notifications` | 消息通知 | 通知列表            | 是       | `notifications.view` |
| `/users`         | 用户管理 | 管理员 CRUD         | 是       | `users.view`         |
| `/roles`         | 角色管理 | 角色+权限           | 是       | `roles.view`         |

## 认证机制

- **路由守卫**：所有需要认证的页面在 `router.beforeEach` 中校验登录状态，未登录自动跳转 `/login`
- **权限校验**：路由 `meta.permission` 定义所需权限码，守卫中调用 `usePermission` 校验
- **API 拦截**：`api.ts` 在收到 HTTP 401 响应时自动清除登录态并跳转登录页
- **Token 存储**：JWT 存储在 `localStorage`，每次请求通过 `Authorization: Bearer` 头携带
- **默认进入**：访问根路径 `/` 自动重定向到 `/dashboard`；已登录用户访问 `/login` 自动重定向到 `/dashboard`

## 两类"用户"的区别

| 类型          | 对应页面              | 能否登录系统 | 说明                                   |
| ------------- | --------------------- | ------------ | -------------------------------------- |
| **后台用户**  | `系统管理 → 用户管理` | ✅ 能         | 图书馆工作人员，登录后台进行日常操作   |
| **读者/会员** | `读者管理`            | ❌ 不能       | 图书馆的借书人，由管理员代办借还等操作 |

当前系统是**后台管理系统**，仅供图书馆工作人员使用。读者没有自助门户，所有借书、还书、预约、罚款等操作均由管理员在后台代为办理。

## 默认账号

系统首次启动时会自动种子以下后台登录账号：

| 角色       | 用户名      | 密码             | 说明                          |
| ---------- | ----------- | ---------------- | ----------------------------- |
| 系统管理员 | `admin`     | `Admin@123456`   | 拥有全部权限                  |
| 图书管理员 | `librarian` | `Library@123456` | 图书/借阅/会员/罚款等管理权限 |

> 以上账号密码可在首次登录后通过「系统管理 → 用户管理」修改。
