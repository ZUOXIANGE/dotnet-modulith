<template>
  <div class="page-container">
    <div class="page-header">
      <span>读者管理</span>
      <n-button type="primary" v-if="hasPermission('members.manage')" @click="showCreateDialog = true">新增读者</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-input v-model:value="keyword" placeholder="搜索姓名/手机号/邮箱" clearable style="width: 260px" @keyup.enter="search" />
          <n-select v-model:value="statusFilter" placeholder="状态筛选" clearable :options="statusOptions" style="width: 140px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="members" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>

    <n-modal v-model:show="showCreateDialog" title="新增读者" preset="card" style="width: 560px" :mask-closable="false">
      <n-form ref="formRef" :model="form" :rules="rules" label-placement="left" label-width="90">
        <n-form-item label="姓名" path="name">
          <n-input v-model:value="form.name" placeholder="请输入姓名" />
        </n-form-item>
        <n-form-item label="手机号" path="phone">
          <n-input v-model:value="form.phone" placeholder="请输入手机号" />
        </n-form-item>
        <n-form-item label="邮箱" path="email">
          <n-input v-model:value="form.email" placeholder="请输入邮箱" />
        </n-form-item>
        <n-form-item label="地址" path="address">
          <n-input v-model:value="form.address" placeholder="请输入地址" />
        </n-form-item>
        <n-form-item label="会员类型" path="membershipType">
          <n-select v-model:value="form.membershipType" placeholder="请选择会员类型" :options="membershipTypeOptions" />
        </n-form-item>
        <n-form-item label="入会日期" path="joinDate">
          <n-date-picker v-model:formatted-value="form.joinDate" type="date" value-format="yyyy-MM-dd" />
        </n-form-item>
        <n-form-item label="有效期至" path="expiryDate">
          <n-date-picker v-model:formatted-value="form.expiryDate" type="date" value-format="yyyy-MM-dd" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showCreateDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleCreate">确认</n-button>
        </n-space>
      </template>
    </n-modal>

    <n-modal v-model:show="showEditDialog" title="编辑读者" preset="card" style="width: 560px" :mask-closable="false">
      <n-form ref="editFormRef" :model="editForm" :rules="rules" label-placement="left" label-width="90">
        <n-form-item label="姓名" path="name">
          <n-input v-model:value="editForm.name" placeholder="请输入姓名" />
        </n-form-item>
        <n-form-item label="手机号" path="phone">
          <n-input v-model:value="editForm.phone" placeholder="请输入手机号" />
        </n-form-item>
        <n-form-item label="邮箱" path="email">
          <n-input v-model:value="editForm.email" placeholder="请输入邮箱" />
        </n-form-item>
        <n-form-item label="地址" path="address">
          <n-input v-model:value="editForm.address" placeholder="请输入地址" />
        </n-form-item>
        <n-form-item label="会员类型" path="membershipType">
          <n-select v-model:value="editForm.membershipType" placeholder="请选择会员类型" :options="membershipTypeOptions" />
        </n-form-item>
        <n-form-item label="有效期至" path="expiryDate">
          <n-date-picker v-model:formatted-value="editForm.expiryDate" type="date" value-format="yyyy-MM-dd" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showEditDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleUpdate">确认</n-button>
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

interface MemberItem {
  id: string
  name: string
  phone: string
  email: string
  membershipType: string
  status: string
  maxBorrowCount: number
  currentBorrowCount: number
  joinDate: string
  expiryDate: string | null
  createdAt: string
}

const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()
const loading = ref(false)
const submitting = ref(false)
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const formRef = ref<FormInst | null>(null)
const editFormRef = ref<FormInst | null>(null)
const editingId = ref<string | null>(null)

const keyword = ref('')
const statusFilter = ref<string | null>(null)
const statusOptions = [
  { label: '正常', value: 'Active' },
  { label: '已过期', value: 'Expired' },
  { label: '已停用', value: 'Suspended' },
  { label: '已注销', value: 'Cancelled' }
]
const membershipTypeOptions = [
  { label: '普通会员', value: 'Normal' },
  { label: '学生', value: 'Student' },
  { label: '教师', value: 'Teacher' },
  { label: 'VIP', value: 'Vip' }
]

const members = ref<MemberItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    pagination.page = page
    fetchMembers()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchMembers()
  }
})

const statusTagMap: Record<string, { type: 'success' | 'warning' | 'error' | 'default'; label: string }> = {
  Active: { type: 'success', label: '正常' },
  Expired: { type: 'warning', label: '已过期' },
  Suspended: { type: 'error', label: '已停用' },
  Cancelled: { type: 'default', label: '已注销' }
}

const membershipTypeTagMap: Record<string, string> = {
  Normal: '普通',
  Student: '学生',
  Teacher: '教师',
  Vip: 'VIP'
}

const columns: DataTableColumns<MemberItem> = [
  { title: '姓名', key: 'name', width: 120 },
  { title: '手机号', key: 'phone', width: 130 },
  { title: '邮箱', key: 'email', width: 180 },
  {
    title: '会员类型',
    key: 'membershipType',
    width: 90,
    render: (row) => membershipTypeTagMap[row.membershipType] ?? row.membershipType
  },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render(row) {
      const tag = statusTagMap[row.status]
      return h(NTag, { type: tag?.type ?? 'default', size: 'small' }, { default: () => tag?.label ?? row.status })
    }
  },
  { title: '借阅', key: 'currentBorrowCount', width: 80, render: (row) => `${row.currentBorrowCount}/${row.maxBorrowCount}` },
  { title: '入会日期', key: 'joinDate', width: 110 },
  {
    title: '操作',
    key: 'actions',
    width: 200,
    render(row) {
      if (!hasPermission('members.manage')) return null
      const buttons = [
        h(NButton, { size: 'small', onClick: () => startEdit(row) }, { default: () => '编辑' })
      ]
      if (row.status === 'Active') {
        buttons.push(
          h(NButton, { size: 'small', type: 'warning', onClick: () => handleSuspend(row) }, { default: () => '停用' })
        )
      }
      if (row.status === 'Suspended') {
        buttons.push(
          h(NButton, { size: 'small', type: 'success', onClick: () => handleActivate(row) }, { default: () => '启用' })
        )
      }
      if (row.status !== 'Cancelled') {
        buttons.push(
          h(NButton, { size: 'small', type: 'error', onClick: () => handleDelete(row) }, { default: () => '注销' })
        )
      }
      return h(NSpace, {}, { default: () => buttons })
    }
  }
]

const form = reactive({
  name: '',
  phone: '',
  email: '',
  address: '',
  membershipType: 'Normal' as string,
  joinDate: '',
  expiryDate: '' as string | null
})

const editForm = reactive({
  name: '',
  phone: '',
  email: '',
  address: '',
  membershipType: 'Normal' as string,
  expiryDate: '' as string | null
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  phone: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
  email: [
    { required: true, message: '请输入邮箱', trigger: 'blur' },
    { type: 'email', message: '请输入有效邮箱', trigger: 'blur' }
  ],
  membershipType: [{ required: true, message: '请选择会员类型', trigger: 'blur' }]
}

async function fetchMembers() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (keyword.value) params.set('keyword', keyword.value)
    if (statusFilter.value) params.set('status', statusFilter.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: MemberItem[]; total: number }>(`/members?${params.toString()}`)
    if (res.code === 200 && res.data) {
      members.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取读者列表失败')
  } finally {
    loading.value = false
  }
}

function search() {
  pagination.page = 1
  fetchMembers()
}

function resetForm() {
  form.name = ''
  form.phone = ''
  form.email = ''
  form.address = ''
  form.membershipType = 'Normal'
  form.joinDate = ''
  form.expiryDate = null
}

async function handleCreate() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/members', {
      name: form.name,
      phone: form.phone,
      email: form.email,
      address: form.address,
      membershipType: form.membershipType,
      joinDate: form.joinDate || undefined,
      expiryDate: form.expiryDate || undefined
    })
    if (res.code === 200) {
      message.success('新增读者成功')
      showCreateDialog.value = false
      resetForm()
      fetchMembers()
    } else {
      message.error(res.msg || '新增失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function startEdit(row: MemberItem) {
  editingId.value = row.id
  editForm.name = row.name
  editForm.phone = row.phone
  editForm.email = row.email
  editForm.address = ''
  editForm.membershipType = row.membershipType
  editForm.expiryDate = row.expiryDate
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
    const res = await api.put(`/members/${editingId.value}`, {
      name: editForm.name,
      phone: editForm.phone,
      email: editForm.email,
      address: editForm.address,
      membershipType: editForm.membershipType,
      expiryDate: editForm.expiryDate || undefined
    })
    if (res.code === 200) {
      message.success('更新成功')
      showEditDialog.value = false
      fetchMembers()
    } else {
      message.error(res.msg || '更新失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function handleDelete(row: MemberItem) {
  dialog.warning({
    title: '确认注销',
    content: `确定要注销读者"${row.name}"吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.delete(`/members/${row.id}`)
      if (res.code === 200) {
        message.success('注销成功')
        fetchMembers()
      } else {
        message.error(res.msg || '注销失败')
      }
    }
  })
}

async function handleSuspend(row: MemberItem) {
  dialog.warning({
    title: '确认停用',
    content: `确定要停用读者"${row.name}"吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/members/${row.id}/suspend`)
      if (res.code === 200) {
        message.success('停用成功')
        fetchMembers()
      } else {
        message.error(res.msg || '停用失败')
      }
    }
  })
}

async function handleActivate(row: MemberItem) {
  const res = await api.post(`/members/${row.id}/activate`)
  if (res.code === 200) {
    message.success('启用成功')
    fetchMembers()
  } else {
    message.error(res.msg || '启用失败')
  }
}

onMounted(() => {
  fetchMembers()
})
</script>
