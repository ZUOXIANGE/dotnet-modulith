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

    <n-modal v-model:show="showChangePassword" title="修改密码" preset="card" style="width: 420px" :mask-closable="false">
      <n-form ref="passwordFormRef" :model="passwordForm" :rules="passwordRules" label-placement="left" label-width="90">
        <n-form-item label="当前密码" path="currentPassword">
          <n-input v-model:value="passwordForm.currentPassword" type="password" placeholder="请输入当前密码" />
        </n-form-item>
        <n-form-item label="新密码" path="newPassword">
          <n-input v-model:value="passwordForm.newPassword" type="password" placeholder="请输入新密码（至少8位）" />
        </n-form-item>
        <n-form-item label="确认密码" path="confirmPassword">
          <n-input v-model:value="passwordForm.confirmPassword" type="password" placeholder="请再次输入新密码" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showChangePassword = false">取消</n-button>
          <n-button type="primary" :loading="passwordSubmitting" @click="handleChangePassword">确认修改</n-button>
        </n-space>
      </template>
    </n-modal>
  </n-layout>
</template>

<script setup lang="ts">
import { ref, computed, h, reactive } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { NIcon, useMessage, type FormInst, type FormRules } from 'naive-ui'
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
import { api } from '@/utils/api'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()
const message = useMessage()
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
  } else if (key === 'changePassword') {
    showChangePassword.value = true
  }
}

const showChangePassword = ref(false)
const passwordSubmitting = ref(false)
const passwordFormRef = ref<FormInst | null>(null)

const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})

function validateConfirmPassword(_rule: any, value: string) {
  if (!value) return new Error('请再次输入新密码')
  if (value !== passwordForm.newPassword) return new Error('两次输入的密码不一致')
  return true
}

const passwordRules: FormRules = {
  currentPassword: [{ required: true, message: '请输入当前密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码至少8位', trigger: 'blur' }
  ],
  confirmPassword: [{ validator: validateConfirmPassword, trigger: 'blur' }]
}

async function handleChangePassword() {
  try {
    await passwordFormRef.value?.validate()
  } catch {
    return
  }

  passwordSubmitting.value = true
  try {
    const res = await api.post('/auth/change-password', {
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword
    })
    if (res.code === 200) {
      message.success('密码修改成功，请重新登录')
      showChangePassword.value = false
      passwordForm.currentPassword = ''
      passwordForm.newPassword = ''
      passwordForm.confirmPassword = ''
      authStore.logout()
      router.push('/login')
    } else {
      message.error(res.msg || '修改失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    passwordSubmitting.value = false
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
