<template>
  <div class="page-container">
    <div class="page-header">
      <span>角色管理</span>
      <n-button type="primary" @click="openCreateDialog">新增角色</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-data-table :columns="columns" :data="roles" :loading="loading" />
      </n-card>
    </n-space>

    <n-modal v-model:show="showFormDialog" :title="editingId ? '编辑角色' : '新增角色'" preset="card" style="width: 600px" :mask-closable="false">
      <n-form ref="formRef" :model="form" :rules="formRules" label-placement="left" label-width="90">
        <n-form-item label="角色名称" path="name">
          <n-input v-model:value="form.name" placeholder="请输入角色名称" :maxlength="100" show-count />
        </n-form-item>
        <n-form-item label="角色描述" path="description">
          <n-input
            v-model:value="form.description"
            placeholder="请输入角色描述"
            type="textarea"
            :autosize="{ minRows: 2, maxRows: 4 }"
            :maxlength="500"
            show-count
          />
        </n-form-item>
        <n-form-item label="角色权限" path="permissions">
          <n-checkbox-group v-model:value="form.permissions">
            <n-grid :cols="2" :x-gap="16" :y-gap="4">
              <n-gi v-for="p in permissionItems" :key="p.code">
                <n-checkbox :value="p.code">
                  <n-space align="center" :size="4">
                    <span>{{ p.name }}</span>
                    <n-tag :bordered="false" size="tiny" type="default">{{ p.code }}</n-tag>
                  </n-space>
                </n-checkbox>
              </n-gi>
            </n-grid>
          </n-checkbox-group>
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showFormDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleSubmit">确认</n-button>
        </n-space>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, h } from 'vue'
import { useMessage, type FormInst, type FormRules, type DataTableColumns, NButton, NSpace, NTag, NCheckbox, NCheckboxGroup, NGrid, NGi } from 'naive-ui'
import { api } from '@/utils/api'

interface RoleItem {
  id: string
  name: string
  description: string | null
  isSystem: boolean
  permissions: string[]
}

interface PermissionItem {
  code: string
  name: string
  description: string
}

const message = useMessage()
const loading = ref(false)
const submitting = ref(false)
const showFormDialog = ref(false)
const formRef = ref<FormInst | null>(null)
const editingId = ref<string | null>(null)

const roles = ref<RoleItem[]>([])
const permissionItems = ref<PermissionItem[]>([])

const form = reactive({
  name: '',
  description: '',
  permissions: [] as string[]
})

const formRules: FormRules = {
  name: [{ required: true, message: '请输入角色名称', trigger: 'blur' }]
}

const columns: DataTableColumns<RoleItem> = [
  {
    title: '角色名称',
    key: 'name',
    width: 150,
    render(row) {
      return h(
        'div',
        { style: { display: 'flex', alignItems: 'center', gap: '6px' } },
        [
          row.name,
          row.isSystem
            ? h(NTag, { type: 'info', size: 'small', bordered: false }, { default: () => '系统' })
            : null
        ]
      )
    }
  },
  {
    title: '描述',
    key: 'description',
    width: 220,
    ellipsis: { tooltip: true },
    render(row) { return row.description || '-' }
  },
  {
    title: '权限',
    key: 'permissions',
    render(row) {
      if (!row.permissions || row.permissions.length === 0) return '-'
      const maxShow = 4
      const shown = row.permissions.slice(0, maxShow)
      const more = row.permissions.length - maxShow
      return h(NSpace, { size: 4 }, {
        default: () => [
          ...shown.map(p => h(NTag, { size: 'small', bordered: false }, { default: () => p })),
          more > 0 ? h(NTag, { size: 'small', bordered: false }, { default: () => `+${more}` }) : null
        ]
      })
    }
  },
  {
    title: '操作',
    key: 'actions',
    width: 120,
    render(row) {
      if (row.isSystem) return '-'
      return h(NSpace, { size: 4 }, {
        default: () => [
          h(NButton, { size: 'small', onClick: () => openEditDialog(row) }, { default: () => '编辑' })
        ]
      })
    }
  }
]

function resetForm() {
  form.name = ''
  form.description = ''
  form.permissions = []
  editingId.value = null
  formRef.value?.restoreValidation()
}

function openCreateDialog() {
  resetForm()
  showFormDialog.value = true
}

function openEditDialog(row: RoleItem) {
  editingId.value = row.id
  form.name = row.name
  form.description = row.description || ''
  form.permissions = [...row.permissions]
  showFormDialog.value = true
}

async function fetchRoles() {
  loading.value = true
  try {
    const res = await api.get<RoleItem[]>('/roles')
    if (res.code === 200 && res.data) {
      roles.value = res.data
    }
  } catch {
    message.error('获取角色列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchPermissions() {
  try {
    const res = await api.get<PermissionItem[]>('/roles/permissions')
    if (res.code === 200 && res.data) {
      permissionItems.value = res.data
    }
  } catch {
    // ignore
  }
}

async function handleSubmit() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const payload = {
      name: form.name,
      description: form.description || undefined,
      permissions: form.permissions
    }

    if (editingId.value) {
      const res = await api.put(`/roles/${editingId.value}`, payload)
      if (res.code === 200) {
        message.success('角色更新成功')
        showFormDialog.value = false
        fetchRoles()
      } else {
        message.error(res.msg || '更新失败')
      }
    } else {
      const res = await api.post('/roles', payload)
      if (res.code === 200) {
        message.success('创建角色成功')
        showFormDialog.value = false
        fetchRoles()
      } else {
        message.error(res.msg || '创建失败')
      }
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  fetchRoles()
  fetchPermissions()
})
</script>
