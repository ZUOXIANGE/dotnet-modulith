<template>
  <div class="page-container">
    <div class="page-header">
      <span>预约管理</span>
      <n-button type="primary" v-if="hasPermission('reservation.manage')" @click="showCreateDialog = true">新建预约</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-select v-model:value="statusFilter" placeholder="状态筛选" clearable :options="statusOptions" style="width: 140px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="reservations" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>

    <n-modal v-model:show="showCreateDialog" title="新建预约" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="createFormRef" :model="createForm" :rules="createRules" label-placement="left" label-width="90">
        <n-form-item label="图书" path="bookId">
          <SelectorPopup
            v-model="createForm.bookId"
            title="选择图书"
            placeholder="请选择图书"
            search-placeholder="搜索图书名称或ISBN"
            api-url="/books"
            :columns="bookColumns"
            display-field="title"
            :label-formatter="(b: any) => `${b.title} (${b.isbn})`"
          />
        </n-form-item>
        <n-form-item label="读者" path="memberId">
          <SelectorPopup
            v-model="createForm.memberId"
            title="选择读者"
            placeholder="请选择读者"
            search-placeholder="搜索读者姓名或电话"
            api-url="/members"
            :columns="memberColumns"
            display-field="name"
            :label-formatter="(m: any) => `${m.name} (${m.phone})`"
          />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showCreateDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleCreate">确认预约</n-button>
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
import SelectorPopup from '@/components/SelectorPopup.vue'

interface ReservationItem {
  id: string
  bookId: string
  bookTitle: string
  memberId: string
  memberName: string
  reserveDate: string
  expiryDate: string
  status: string
  queuePosition: number
  createdAt: string
}

const message = useMessage()
const dialog = useDialog()
const { hasPermission } = usePermission()
const loading = ref(false)
const submitting = ref(false)
const showCreateDialog = ref(false)
const createFormRef = ref<FormInst | null>(null)

const statusFilter = ref<string | null>(null)
const statusOptions = [
  { label: '待处理', value: 'Pending' },
  { label: '已履约', value: 'Fulfilled' },
  { label: '已取消', value: 'Cancelled' },
  { label: '已过期', value: 'Expired' }
]

const reservations = ref<ReservationItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    pagination.page = page
    fetchReservations()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchReservations()
  }
})

const statusTagMap: Record<string, { type: 'info' | 'success' | 'default' | 'warning'; label: string }> = {
  Pending: { type: 'info', label: '待处理' },
  Fulfilled: { type: 'success', label: '已履约' },
  Cancelled: { type: 'default', label: '已取消' },
  Expired: { type: 'warning', label: '已过期' }
}

const columns: DataTableColumns<ReservationItem> = [
  { title: '图书', key: 'bookTitle', width: 200 },
  { title: '读者', key: 'memberName', width: 120 },
  { title: '预约日期', key: 'reserveDate', width: 110 },
  { title: '截止日期', key: 'expiryDate', width: 110 },
  {
    title: '状态',
    key: 'status',
    width: 90,
    render(row) {
      const tag = statusTagMap[row.status]
      return h(NTag, { type: tag?.type ?? 'default', size: 'small' }, { default: () => tag?.label ?? row.status })
    }
  },
  { title: '排队位置', key: 'queuePosition', width: 90 },
  { title: '创建时间', key: 'createdAt', width: 160, render(row) { return new Date(row.createdAt).toLocaleString() } },
  {
    title: '操作',
    key: 'actions',
    width: 100,
    render(row) {
      if (!hasPermission('reservation.manage')) return null
      if (row.status === 'Pending') {
        return h(NButton, { size: 'small', type: 'error', onClick: () => handleCancel(row) }, { default: () => '取消预约' })
      }
      return null
    }
  }
]

const createForm = reactive({
  bookId: null as string | null,
  memberId: null as string | null
})

const createRules: FormRules = {
  bookId: [{ required: true, message: '请选择图书', trigger: 'blur' }],
  memberId: [{ required: true, message: '请选择读者', trigger: 'blur' }]
}

const bookColumns: DataTableColumns<any> = [
  { title: '书名', key: 'title', width: 200 },
  { title: 'ISBN', key: 'isbn', width: 140 }
]

const memberColumns: DataTableColumns<any> = [
  { title: '姓名', key: 'name', width: 120 },
  { title: '电话', key: 'phone', width: 140 }
]

async function fetchReservations() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (statusFilter.value) params.set('status', statusFilter.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: ReservationItem[]; total: number }>(`/reservations?${params.toString()}`)
    if (res.code === 200 && res.data) {
      reservations.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取预约列表失败')
  } finally {
    loading.value = false
  }
}

function search() {
  pagination.page = 1
  fetchReservations()
}

async function handleCreate() {
  try {
    await createFormRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/reservations', {
      bookId: createForm.bookId,
      memberId: createForm.memberId
    })
    if (res.code === 200) {
      message.success('预约成功')
      showCreateDialog.value = false
      createForm.bookId = null
      createForm.memberId = null
      fetchReservations()
    } else {
      message.error(res.msg || '预约失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function handleCancel(row: ReservationItem) {
  dialog.warning({
    title: '取消预约',
    content: `确定取消《${row.bookTitle}》的预约吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.delete(`/reservations/${row.id}`)
      if (res.code === 200) {
        message.success('已取消预约')
        fetchReservations()
      } else {
        message.error(res.msg || '取消失败')
      }
    }
  })
}

onMounted(() => {
  fetchReservations()
})
</script>
