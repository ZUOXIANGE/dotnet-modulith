<template>
  <div class="page-container">
    <div class="page-header">
      <span>消息通知</span>
      <n-space>
        <n-badge :value="unreadCount" :max="99" show-zero>
          <n-tag type="info">未读</n-tag>
        </n-badge>
        <n-button type="primary" size="small" @click="handleMarkAllRead">全部已读</n-button>
      </n-space>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-select v-model:value="isReadFilter" placeholder="阅读状态" clearable :options="readOptions" style="width: 140px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="notifications" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, h } from 'vue'
import { useMessage, type DataTableColumns, NButton, NTag } from 'naive-ui'
import { api } from '@/utils/api'

interface NotificationItem {
  id: string
  title: string
  content: string
  type: string
  recipientId: string
  isRead: boolean
  createdAt: string
  readAt: string | null
}

const message = useMessage()
const loading = ref(false)
const unreadCount = ref(0)

const isReadFilter = ref<boolean | null>(null)
const readOptions = [
  { label: '未读', value: false },
  { label: '已读', value: true }
]

const notifications = ref<NotificationItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  onChange: (page: number) => {
    pagination.page = page
    fetchNotifications()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchNotifications()
  }
})

const typeTagMap: Record<string, { type: 'info' | 'warning' | 'success' | 'error' | 'default'; label: string }> = {
  BorrowDue: { type: 'info', label: '借阅到期' },
  Overdue: { type: 'warning', label: '借阅逾期' },
  ReservationAvailable: { type: 'success', label: '预约可借' },
  FineIssued: { type: 'error', label: '罚款通知' },
  System: { type: 'default', label: '系统通知' }
}

const columns: DataTableColumns<NotificationItem> = [
  {
    title: '',
    key: 'isRead',
    width: 40,
    render(row) {
      return h('span', { style: { color: row.isRead ? '#ccc' : '#2080f0', fontSize: '18px', lineHeight: 1 } }, row.isRead ? '●' : '●')
    }
  },
  { title: '标题', key: 'title', width: 180 },
  { title: '内容', key: 'content', ellipsis: { tooltip: true }, width: 280 },
  {
    title: '类型',
    key: 'type',
    width: 100,
    render(row) {
      const tag = typeTagMap[row.type]
      return h(NTag, { type: tag?.type ?? 'default', size: 'small' }, { default: () => tag?.label ?? row.type })
    }
  },
  { title: '时间', key: 'createdAt', width: 160, render(row) { return new Date(row.createdAt).toLocaleString() } },
  {
    title: '操作',
    key: 'actions',
    width: 80,
    render(row) {
      if (!row.isRead) {
        return h(
          NButton,
          { size: 'small', type: 'primary', onClick: () => handleMarkRead(row) },
          { default: () => '标记已读' }
        )
      }
      return null
    }
  }
]

async function fetchNotifications() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (isReadFilter.value !== null) params.set('isRead', String(isReadFilter.value))
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: NotificationItem[]; total: number }>(`/notifications?${params.toString()}`)
    if (res.code === 200 && res.data) {
      notifications.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取通知列表失败')
  } finally {
    loading.value = false
  }
}

async function fetchUnreadCount() {
  try {
    const res = await api.get<{ unreadCount: number }>('/notifications/unread-count')
    if (res.code === 200 && res.data) {
      unreadCount.value = res.data.unreadCount
    }
  } catch {
    // ignore
  }
}

function search() {
  pagination.page = 1
  fetchNotifications()
}

async function handleMarkRead(row: NotificationItem) {
  const res = await api.post(`/notifications/${row.id}/read`)
  if (res.code === 200) {
    message.success('已标记为已读')
    fetchNotifications()
    fetchUnreadCount()
  } else {
    message.error(res.msg || '操作失败')
  }
}

async function handleMarkAllRead() {
  const res = await api.post('/notifications/read-all')
  if (res.code === 200) {
    message.success('全部已读')
    fetchNotifications()
    fetchUnreadCount()
  } else {
    message.error(res.msg || '操作失败')
  }
}

onMounted(() => {
  fetchNotifications()
  fetchUnreadCount()
})
</script>