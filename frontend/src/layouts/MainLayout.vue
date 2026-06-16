<template>
  <n-layout has-sider class="main-layout">
    <n-layout-sider
      bordered
      collapse-mode="width"
      :collapsed-width="64"
      :width="240"
      :collapsed="collapsed"
      show-trigger
      @collapse="collapsed = true"
      @expand="collapsed = false"
    >
      <div class="logo-container">
        <span class="logo-text" v-if="!collapsed">图书馆管理系统</span>
        <span class="logo-text-short" v-else>图书</span>
      </div>
      <n-menu
        :collapsed="collapsed"
        :collapsed-width="64"
        :collapsed-icon-size="22"
        :options="menuOptions"
        :value="activeKey"
        @update:value="handleMenuUpdate"
      />
    </n-layout-sider>
    <n-layout>
      <n-layout-header bordered class="main-header">
        <div class="header-right">
          <n-dropdown :options="userDropdownOptions" @select="handleUserDropdown">
            <n-button text>
              <template #icon>
                <n-icon><person-circle-outline /></n-icon>
              </template>
              {{ authStore.userName || '管理员' }}
            </n-button>
          </n-dropdown>
        </div>
      </n-layout-header>
      <n-layout-content class="main-content">
        <router-view />
      </n-layout-content>
    </n-layout>
  </n-layout>
</template>

<script setup lang="ts">
import { ref, computed, h } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { NIcon } from 'naive-ui'
import { PersonCircleOutline } from '@vicons/ionicons5'
import {
  GridOutline,
  BookOutline,
  PeopleOutline,
  SwapHorizontalOutline,
  CalendarOutline,
  CashOutline,
  BarChartOutline,
  PersonOutline,
  ShieldCheckmarkOutline,
  PricetagsOutline
} from '@vicons/ionicons5'
import { useAuthStore } from '@/stores/auth'
import type { MenuOption } from 'naive-ui'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const collapsed = ref(false)

const renderIcon = (icon: any) => () => h(NIcon, null, { default: () => h(icon) })

const menuOptions: MenuOption[] = [
  { label: '工作台', key: 'dashboard', icon: renderIcon(GridOutline) },
  {
    label: '图书管理',
    key: 'books-group',
    icon: renderIcon(BookOutline),
    children: [
      { label: '图书列表', key: 'books' },
      { label: '分类管理', key: 'categories' }
    ]
  },
  { label: '读者管理', key: 'members', icon: renderIcon(PeopleOutline) },
  {
    label: '借还管理',
    key: 'borrowing-group',
    icon: renderIcon(SwapHorizontalOutline),
    children: [
      { label: '借还操作', key: 'borrowing' },
      { label: '预约管理', key: 'reservations' }
    ]
  },
  { label: '罚款管理', key: 'fines', icon: renderIcon(CashOutline) },
  { label: '统计报表', key: 'reports', icon: renderIcon(BarChartOutline) },
  {
    label: '系统管理',
    key: 'system-group',
    icon: renderIcon(ShieldCheckmarkOutline),
    children: [
      { label: '用户管理', key: 'users' },
      { label: '角色管理', key: 'roles' }
    ]
  }
]

const activeKey = computed(() => {
  const path = route.path
  if (path.startsWith('/books')) return 'books'
  if (path.startsWith('/categories')) return 'categories'
  if (path.startsWith('/members')) return 'members'
  if (path.startsWith('/borrowing')) return 'borrowing'
  if (path.startsWith('/reservations')) return 'reservations'
  if (path.startsWith('/fines')) return 'fines'
  if (path.startsWith('/reports')) return 'reports'
  if (path.startsWith('/users')) return 'users'
  if (path.startsWith('/roles')) return 'roles'
  return 'dashboard'
})

const userDropdownOptions = [
  { label: '修改密码', key: 'changePassword' },
  { label: '退出登录', key: 'logout' }
]

function handleMenuUpdate(key: string) {
  router.push(`/${key}`)
}

function handleUserDropdown(key: string) {
  if (key === 'logout') {
    authStore.logout()
    router.push('/login')
  }
}
</script>

<style scoped>
.main-layout {
  height: 100vh;
}

.logo-container {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-bottom: 1px solid var(--n-border-color);
}

.logo-text {
  font-size: 16px;
  font-weight: 600;
  white-space: nowrap;
}

.logo-text-short {
  font-size: 14px;
  font-weight: 600;
}

.main-header {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: flex-end;
  padding: 0 24px;
}

.main-content {
  background: #f5f7fa;
  min-height: calc(100vh - 56px);
}
</style>