<template>
  <div class="page-container">
    <div class="page-header">
      <span>用户管理</span>
      <n-button type="primary" v-if="hasPermission('users.manage')" @click="showCreateDialog = true">新增用户</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-data-table :columns="columns" :data="users" :loading="loading" />
      </n-card>
    </n-space>

    <n-modal v-model:show="showCreateDialog" title="新增用户" preset="card" style="width: 520px" :mask-closable="false">
      <n-form ref="createFormRef" :model="createForm" :rules="createRules" label-placement="left" label-width="90">
        <n-form-item label="用户名" path="userName">
          <n-input v-model:value="createForm.userName" placeholder="请输入用户名" />
        </n-form-item>
        <n-form-item label="显示名称" path="displayName">
          <n-input v-model:value="createForm.displayName" placeholder="请输入显示名称" />
        </n-form-item>
        <n-form-item label="邮箱" path="email">
          <n-input v-model:value="createForm.email" placeholder="请输入邮箱" />
        </n-form-item>
        <n-form-item label="密码" path="password">
          <n-input v-model:value="createForm.password" type="password" placeholder="请输入密码（至少8位）" />
        </n-form-item>
        <n-form-item label="角色" path="roleIds">
          <n-select v-model:value="createForm.roleIds" multiple placeholder="请选择角色" :options="roleOptions" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showCreateDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleCreate">确认</n-button>
        </n-space>
      </template>
    </n-modal>

    <n-modal v-model:show="showEditDialog" title="编辑用户" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="editFormRef" :model="editForm" :rules="editRules" label-placement="left" label-width="90">
        <n-form-item label="显示名称" path="displayName">
          <n-input v-model:value="editForm.displayName" placeholder="请输入显示名称" />
        </n-form-item>
        <n-form-item label="邮箱" path="email">
          <n-input v-model:value="editForm.email" placeholder="请输入邮箱" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showEditDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleUpdate">确认</n-button>
        </n-space>
      </template>
    </n-modal>

    <n-modal v-model:show="showRolesDialog" title="分配角色" preset="card" style="width: 480px" :mask-closable="false">
      <n-form label-placement="left" label-width="90">
        <n-form-item label="角色">
          <n-select v-model:value="assignRoleIds" multiple placeholder="请选择角色" :options="roleOptions" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showRolesDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleAssignRoles">确认</n-button>
        </n-space>
      </template>
    </n-modal>

    <n-modal v-model:show="showResetDialog" title="重置密码" preset="card" style="width: 420px" :mask-closable="false">
      <n-form ref="resetFormRef" :model="resetForm" :rules="resetRules" label-placement="left" label-width="90">
        <n-form-item label="新密码" path="password">
          <n-input v-model:value="resetForm.password" type="password" placeholder="请输入新密码（至少8位）" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showResetDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleResetPassword">确认</n-button>
        </n-space>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, h } from 'vue'
import { useMessage, useDialog, type FormInst, type FormRules, type DataTableColumns, NButton, NSpace, NTag } from 'naive-ui'
import { api } from '@/utils/api'
import { usePermission } from '@/composables/usePermission'

interface UserItem {
  id: string
  userName: string
  displayName: string
  email: string
  isActive: boolean
  createdAt: string
  lastLoginAt: string | null
  roles: string[]
}

interface RoleItem {
  id: string
  name: string
  description: string | null
  isSystem: boolean
  permissions: string[]
}

const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()
const loading = ref(false)
const submitting = ref(false)
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const showRolesDialog = ref(false)
const showResetDialog = ref(false)
const createFormRef = ref<FormInst | null>(null)
const editFormRef = ref<FormInst | null>(null)
const resetFormRef = ref<FormInst | null>(null)

const editingId = ref<string | null>(null)
const assignRoleIds = ref<string[]>([])
const roleOptions = ref<{ label: string; value: string }[]>([])

const users = ref<UserItem[]>([])

const columns: DataTableColumns<UserItem> = [
  { title: '用户名', key: 'userName', width: 130 },
  { title: '显示名称', key: 'displayName', width: 120 },
  { title: '邮箱', key: 'email', width: 200 },
  {
    title: '状态',
    key: 'isActive',
    width: 80,
    render(row) {
      return h(NTag, { type: row.isActive ? 'success' : 'error', size: 'small' }, { default: () => row.isActive ? '正常' : '已禁用' })
    }
  },
  {
    title: '角色',
    key: 'roles',
    width: 150,
    render(row) {
      if (!row.roles || row.roles.length === 0) return '-'
      return h(NSpace, { size: 4 }, {
        default: () => row.roles.map(r => h(NTag, { size: 'small', bordered: false }, { default: () => r }))
      })
    }
  },
  { title: '创建时间', key: 'createdAt', width: 160, render(row) { return new Date(row.createdAt).toLocaleString() } },
  { title: '最后登录', key: 'lastLoginAt', width: 160, render(row) { return row.lastLoginAt ? new Date(row.lastLoginAt).toLocaleString() : '-' } },
  {
    title: '操作',
    key: 'actions',
    width: 280,
    render(row) {
      if (!hasPermission('users.manage')) return null
      const buttons = [
        h(NButton, { size: 'small', onClick: () => startEdit(row) }, { default: () => '编辑' }),
        h(NButton, { size: 'small', onClick: () => startAssignRoles(row) }, { default: () => '角色' })
      ]
      if (row.isActive) {
        buttons.push(
          h(NButton, { size: 'small', type: 'warning', onClick: () => handleDisable(row) }, { default: () => '禁用' })
        )
      } else {
        buttons.push(
          h(NButton, { size: 'small', type: 'success', onClick: () => handleEnable(row) }, { default: () => '启用' })
        )
      }
      buttons.push(
        h(NButton, { size: 'small', onClick: () => startResetPassword(row) }, { default: () => '重置密码' }),
        h(NButton, { size: 'small', type: 'error', onClick: () => handleForceLogout(row) }, { default: () => '强制登出' })
      )
      return h(NSpace, { size: 4 }, { default: () => buttons })
    }
  }
]

const createForm = reactive({
  userName: '',
  displayName: '',
  email: '',
  password: '',
  roleIds: [] as string[]
})

const createRules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入有效邮箱', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 8, message: '密码至少8位', trigger: 'blur' }
  ]
}

const editForm = reactive({
  displayName: '',
  email: ''
})

const editRules: FormRules = {
  displayName: [{ required: true, message: '请输入显示名称', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入有效邮箱', trigger: 'blur' }
  ]
}

const resetForm = reactive({
  password: ''
})

const resetRules: FormRules = {
  password: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 8, message: '密码至少8位', trigger: 'blur' }
  ]
}

async function fetchUsers() {
  loading.value = true
  try {
    const res = await api.get<UserItem[]>('/users')
    if (res.code === 200 && res.data) {
      users.value = res.data
    }
  } catch {
    message.error('获取用户列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchRoles() {
  try {
    const res = await api.get<RoleItem[]>('/roles')
    if (res.code === 200 && res.data) {
      roleOptions.value = res.data.map(r => ({ label: r.name, value: r.id }))
    }
  } catch {
    // ignore
  }
}

async function handleCreate() {
  try {
    await createFormRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/users', {
      userName: createForm.userName,
      displayName: createForm.displayName,
      email: createForm.email,
      password: createForm.password,
      roleIds: createForm.roleIds
    })
    if (res.code === 200) {
      message.success('创建用户成功')
      showCreateDialog.value = false
      createForm.userName = ''
      createForm.displayName = ''
      createForm.email = ''
      createForm.password = ''
      createForm.roleIds = []
      fetchUsers()
    } else {
      message.error(res.msg || '创建失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function startEdit(row: UserItem) {
  editingId.value = row.id
  editForm.displayName = row.displayName
  editForm.email = row.email
  showEditDialog.value = true
}

async function handleUpdate() {
  try {
    await editFormRef.value?.validate()
  } catch {
    return
  }
  if (!editingId.value) return

  submitting.value = true
  try {
    const res = await api.put(`/users/${editingId.value}`, {
      displayName: editForm.displayName,
      email: editForm.email
    })
    if (res.code === 200) {
      message.success('更新成功')
      showEditDialog.value = false
      fetchUsers()
    } else {
      message.error(res.msg || '更新失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function startAssignRoles(row: UserItem) {
  editingId.value = row.id
  assignRoleIds.value = []
  showRolesDialog.value = true
}

async function handleAssignRoles() {
  if (!editingId.value) return

  submitting.value = true
  try {
    const res = await api.put(`/users/${editingId.value}/roles`, {
      roleIds: assignRoleIds.value
    })
    if (res.code === 200) {
      message.success('角色分配成功')
      showRolesDialog.value = false
      fetchUsers()
    } else {
      message.error(res.msg || '分配失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

async function handleDisable(row: UserItem) {
  dialog.warning({
    title: '确认禁用',
    content: `确定要禁用用户"${row.displayName}"吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.put(`/users/${row.id}/status`, { isActive: false })
      if (res.code === 200) {
        message.success('已禁用')
        fetchUsers()
      } else {
        message.error(res.msg || '操作失败')
      }
    }
  })
}

async function handleEnable(row: UserItem) {
  const res = await api.put(`/users/${row.id}/status`, { isActive: true })
  if (res.code === 200) {
    message.success('已启用')
    fetchUsers()
  } else {
    message.error(res.msg || '操作失败')
  }
}

function startResetPassword(row: UserItem) {
  editingId.value = row.id
  resetForm.password = ''
  showResetDialog.value = true
}

async function handleResetPassword() {
  try {
    await resetFormRef.value?.validate()
  } catch {
    return
  }
  if (!editingId.value) return

  submitting.value = true
  try {
    const res = await api.post(`/users/${editingId.value}/reset-password`, {
      password: resetForm.password
    })
    if (res.code === 200) {
      message.success('密码重置成功')
      showResetDialog.value = false
    } else {
      message.error(res.msg || '重置失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function handleForceLogout(row: UserItem) {
  dialog.warning({
    title: '强制登出',
    content: `确定要强制登出用户"${row.displayName}"吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/users/${row.id}/force-logout`, { reason: '管理操作' })
      if (res.code === 200) {
        message.success('已强制登出')
      } else {
        message.error(res.msg || '操作失败')
      }
    }
  })
}

onMounted(() => {
  fetchUsers()
  fetchRoles()
})
</script>
