<template>
  <div class="page-container">
    <div class="page-header">
      <span>罚款管理</span>
      <n-button type="primary" @click="showCreateDialog = true">创建罚款</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-select v-model:value="statusFilter" placeholder="状态筛选" clearable :options="statusOptions" style="width: 140px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="fines" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>

    <n-modal v-model:show="showCreateDialog" title="创建罚款" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="createFormRef" :model="createForm" :rules="createRules" label-placement="left" label-width="90">
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
        <n-form-item label="罚款类型" path="reason">
          <n-select v-model:value="createForm.reason" :options="reasonOptions" />
        </n-form-item>
        <n-form-item label="金额" path="amount">
          <n-input-number v-model:value="createForm.amount" :min="0.01" :step="0.01" style="width: 100%" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showCreateDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleCreate">确认创建</n-button>
        </n-space>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, h } from 'vue'
import { useMessage, useDialog, type FormInst, type FormRules, type DataTableColumns, NButton, NSpace, NTag } from 'naive-ui'
import { api } from '@/utils/api'
import SelectorPopup from '@/components/SelectorPopup.vue'

interface FineItem {
  id: string
  memberId: string
  memberName: string
  borrowingRecordId: string | null
  amount: number
  reason: string
  status: string
  createdAt: string
  paidAt: string | null
}

const message = useMessage()
const dialog = useDialog()
const loading = ref(false)
const submitting = ref(false)
const showCreateDialog = ref(false)
const createFormRef = ref<FormInst | null>(null)

const statusFilter = ref<string | null>(null)
const statusOptions = [
  { label: '未支付', value: 'Unpaid' },
  { label: '已支付', value: 'Paid' },
  { label: '已豁免', value: 'Waived' }
]

const fines = ref<FineItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    pagination.page = page
    fetchFines()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchFines()
  }
})

const reasonTagMap: Record<string, { label: string }> = {
  Overdue: { label: '逾期' },
  Lost: { label: '丢失' },
  Damaged: { label: '损坏' }
}

const statusTagMap: Record<string, { type: 'info' | 'success' | 'warning'; label: string }> = {
  Unpaid: { type: 'info', label: '未支付' },
  Paid: { type: 'success', label: '已支付' },
  Waived: { type: 'warning', label: '已豁免' }
}

const columns: DataTableColumns<FineItem> = [
  { title: '读者', key: 'memberName', width: 120 },
  { title: '金额', key: 'amount', width: 90, render(row) { return `¥${row.amount.toFixed(2)}` } },
  {
    title: '类型',
    key: 'reason',
    width: 80,
    render(row) {
      const tag = reasonTagMap[row.reason]
      return h(NTag, { size: 'small' }, { default: () => tag?.label ?? row.reason })
    }
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
  { title: '创建时间', key: 'createdAt', width: 160, render(row) { return new Date(row.createdAt).toLocaleString() } },
  {
    title: '操作',
    key: 'actions',
    width: 150,
    render(row) {
      if (row.status === 'Unpaid') {
        return h(NSpace, {}, {
          default: () => [
            h(NButton, { size: 'small', type: 'success', onClick: () => handlePay(row) }, { default: () => '支付' }),
            h(NButton, { size: 'small', type: 'warning', onClick: () => handleWaive(row) }, { default: () => '豁免' })
          ]
        })
      }
      return null
    }
  }
]

const createForm = reactive({
  memberId: null as string | null,
  reason: 'Overdue' as string,
  amount: null as number | null
})

const reasonOptions = [
  { label: '逾期', value: 'Overdue' },
  { label: '丢失', value: 'Lost' },
  { label: '损坏', value: 'Damaged' }
]

const createRules: FormRules = {
  memberId: [{ required: true, message: '请选择读者', trigger: 'blur' }],
  reason: [{ required: true, message: '请选择罚款类型', trigger: 'blur' }],
  amount: [
    { required: true, type: 'number', message: '请输入金额', trigger: 'blur' },
    { type: 'number', min: 0.01, message: '金额必须大于0', trigger: 'blur' }
  ]
}

const memberColumns: DataTableColumns<any> = [
  { title: '姓名', key: 'name', width: 120 },
  { title: '电话', key: 'phone', width: 140 }
]

async function fetchFines() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (statusFilter.value) params.set('status', statusFilter.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: FineItem[]; total: number }>(`/fines?${params.toString()}`)
    if (res.code === 200 && res.data) {
      fines.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取罚款列表失败')
  } finally {
    loading.value = false
  }
}

function search() {
  pagination.page = 1
  fetchFines()
}

async function handleCreate() {
  try {
    await createFormRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/fines', {
      memberId: createForm.memberId,
      reason: createForm.reason,
      amount: createForm.amount
    })
    if (res.code === 200) {
      message.success('创建成功')
      showCreateDialog.value = false
      createForm.memberId = null
      createForm.amount = null
      fetchFines()
    } else {
      message.error(res.msg || '创建失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

async function handlePay(row: FineItem) {
  dialog.warning({
    title: '确认支付',
    content: `确定支付 ¥${row.amount.toFixed(2)} 罚款吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/fines/${row.id}/pay`)
      if (res.code === 200) {
        message.success('支付成功')
        fetchFines()
      } else {
        message.error(res.msg || '支付失败')
      }
    }
  })
}

async function handleWaive(row: FineItem) {
  dialog.warning({
    title: '确认豁免',
    content: `确定豁免 ¥${row.amount.toFixed(2)} 罚款吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.post(`/fines/${row.id}/waive`)
      if (res.code === 200) {
        message.success('豁免成功')
        fetchFines()
      } else {
        message.error(res.msg || '豁免失败')
      }
    }
  })
}

onMounted(() => {
  fetchFines()
})
</script>
