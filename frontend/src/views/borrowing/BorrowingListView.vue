<template>
  <div class="page-container">
    <div class="page-header">
      <span>借阅管理</span>
      <n-button type="primary" @click="showBorrowDialog = true">借阅图书</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-select v-model:value="statusFilter" placeholder="状态筛选" clearable :options="statusOptions" style="width: 140px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="borrowings" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>

    <n-modal v-model:show="showBorrowDialog" title="借阅图书" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="borrowFormRef" :model="borrowForm" :rules="borrowRules" label-placement="left" label-width="90">
        <n-form-item label="图书" path="bookId">
          <n-select
            v-model:value="borrowForm.bookId"
            placeholder="搜索并选择图书"
            filterable
            remote
            :loading="bookSearchLoading"
            :options="bookOptions"
            @search="searchBooks"
            @update:value="handleBookSelect"
          />
        </n-form-item>
        <n-form-item label="读者" path="memberId">
          <n-select
            v-model:value="borrowForm.memberId"
            placeholder="搜索并选择读者"
            filterable
            remote
            :loading="memberSearchLoading"
            :options="memberOptions"
            @search="searchMembers"
            @update:value="handleMemberSelect"
          />
        </n-form-item>
        <n-form-item label="借阅天数" path="borrowDays">
          <n-input-number v-model:value="borrowForm.borrowDays" :min="1" :max="60" style="width: 100%" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showBorrowDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleBorrow">确认借阅</n-button>
        </n-space>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, h } from 'vue'
import { useMessage, useDialog, type FormInst, type FormRules, type DataTableColumns, NButton, NSpace, NTag, type SelectOption } from 'naive-ui'
import { api } from '@/utils/api'

interface BorrowingItem {
  id: string
  bookId: string
  bookTitle: string
  memberId: string
  memberName: string
  borrowDate: string
  dueDate: string
  returnDate: string | null
  status: string
  renewalCount: number
  createdAt: string
}

const message = useMessage()
const dialog = useDialog()
const loading = ref(false)
const submitting = ref(false)
const showBorrowDialog = ref(false)
const borrowFormRef = ref<FormInst | null>(null)

const statusFilter = ref<string | null>(null)
const statusOptions = [
  { label: '借阅中', value: 'Borrowed' },
  { label: '已归还', value: 'Returned' },
  { label: '已逾期', value: 'Overdue' },
  { label: '丢失', value: 'Lost' }
]

const borrowings = ref<BorrowingItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    pagination.page = page
    fetchBorrowings()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchBorrowings()
  }
})

const statusTagMap: Record<string, { type: 'info' | 'success' | 'warning' | 'error'; label: string }> = {
  Borrowed: { type: 'info', label: '借阅中' },
  Returned: { type: 'success', label: '已归还' },
  Overdue: { type: 'warning', label: '已逾期' },
  Lost: { type: 'error', label: '丢失' }
}

const columns: DataTableColumns<BorrowingItem> = [
  { title: '图书', key: 'bookTitle', width: 200 },
  { title: '读者', key: 'memberName', width: 120 },
  { title: '借阅日期', key: 'borrowDate', width: 110 },
  { title: '应还日期', key: 'dueDate', width: 110 },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render(row) {
      const tag = statusTagMap[row.status]
      return h(NTag, { type: tag?.type ?? 'default', size: 'small' }, { default: () => tag?.label ?? row.status })
    }
  },
  { title: '续借次数', key: 'renewalCount', width: 80 },
  {
    title: '操作',
    key: 'actions',
    width: 180,
    render(row) {
      if (row.status === 'Borrowed' || row.status === 'Overdue') {
        return h(NSpace, {}, {
          default: () => [
            h(NButton, { size: 'small', type: 'success', onClick: () => handleReturn(row) }, { default: () => '归还' }),
            h(NButton, { size: 'small', type: 'warning', onClick: () => handleRenew(row) }, { default: () => '续借' }),
            h(NButton, { size: 'small', type: 'error', onClick: () => handleMarkLost(row) }, { default: () => '丢失' })
          ]
        })
      }
      return null
    }
  }
]

const borrowForm = reactive({
  bookId: null as string | null,
  memberId: null as string | null,
  borrowDays: 30
})

const borrowRules: FormRules = {
  bookId: [{ required: true, message: '请选择图书', trigger: 'blur' }],
  memberId: [{ required: true, message: '请选择读者', trigger: 'blur' }],
  borrowDays: [
    {
      validator: (_rule, value: unknown) => {
        if (value === null || value === undefined || value === '') {
          return new Error('请输入借阅天数')
        }
        const num = Number(value)
        if (!Number.isFinite(num) || num < 1 || num > 60) {
          return new Error('借阅天数 1-60')
        }
        return true
      },
      trigger: ['blur', 'change']
    }
  ]
}

const bookSearchLoading = ref(false)
const bookOptions = ref<SelectOption[]>([])

const memberSearchLoading = ref(false)
const memberOptions = ref<SelectOption[]>([])

async function fetchBorrowings() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (statusFilter.value) params.set('status', statusFilter.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: BorrowingItem[]; total: number }>(`/borrowings?${params.toString()}`)
    if (res.code === 200 && res.data) {
      borrowings.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取借阅列表失败')
  } finally {
    loading.value = false
  }
}

function search() {
  pagination.page = 1
  fetchBorrowings()
}

async function searchBooks(query: string) {
  if (!query || query.length < 1) {
    bookOptions.value = []
    return
  }
  bookSearchLoading.value = true
  try {
    const res = await api.get<{ items: { id: string; title: string; isbn: string; availableCopies: number }[] }>(`/books?keyword=${encodeURIComponent(query)}&pageSize=10`)
    if (res.code === 200 && res.data) {
      bookOptions.value = res.data.items
        .filter(b => b.availableCopies > 0)
        .map(b => ({ label: `${b.title} (${b.isbn})`, value: b.id }))
    }
  } catch {
    // ignore
  } finally {
    bookSearchLoading.value = false
  }
}

function handleBookSelect() {
  // placeholder
}

async function searchMembers(query: string) {
  if (!query || query.length < 1) {
    memberOptions.value = []
    return
  }
  memberSearchLoading.value = true
  try {
    const res = await api.get<{ items: { id: string; name: string; phone: string }[] }>(`/members?keyword=${encodeURIComponent(query)}&pageSize=10`)
    if (res.code === 200 && res.data) {
      memberOptions.value = res.data.items.map(m => ({ label: `${m.name} (${m.phone})`, value: m.id }))
    }
  } catch {
    // ignore
  } finally {
    memberSearchLoading.value = false
  }
}

function handleMemberSelect() {
  // placeholder
}

async function handleBorrow() {
  try {
    await borrowFormRef.value?.validate()
  } catch (errors) {
    console.log('表单验证失败:', errors)
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/borrowings/borrow', {
      bookId: borrowForm.bookId,
      memberId: borrowForm.memberId,
      borrowDays: borrowForm.borrowDays
    })
    if (res.code === 200) {
      message.success('借阅成功')
      showBorrowDialog.value = false
      borrowForm.bookId = null
      borrowForm.memberId = null
      borrowForm.borrowDays = 30
      fetchBorrowings()
    } else {
      message.error(res.msg || '借阅失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

async function handleReturn(row: BorrowingItem) {
  dialog.warning({
    title: '确认归还',
    content: `确定归还《${row.bookTitle}》吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/borrowings/${row.id}/return`, {})
      if (res.code === 200) {
        message.success('归还成功')
        fetchBorrowings()
      } else {
        message.error(res.msg || '归还失败')
      }
    }
  })
}

async function handleRenew(row: BorrowingItem) {
  dialog.warning({
    title: '确认续借',
    content: `确定续借《${row.bookTitle}》吗？将延长30天`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/borrowings/${row.id}/renew`)
      if (res.code === 200) {
        message.success('续借成功')
        fetchBorrowings()
      } else {
        message.error(res.msg || '续借失败')
      }
    }
  })
}

async function handleMarkLost(row: BorrowingItem) {
  dialog.warning({
    title: '标记丢失',
    content: `确定将《${row.bookTitle}》标记为丢失吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/borrowings/${row.id}/lost`)
      if (res.code === 200) {
        message.success('已标记丢失')
        fetchBorrowings()
      } else {
        message.error(res.msg || '操作失败')
      }
    }
  })
}

onMounted(() => {
  fetchBorrowings()
})
</script>
