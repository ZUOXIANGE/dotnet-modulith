<template>
  <div class="page-container">
    <div class="page-header">
      <span>报表统计</span>
    </div>

    <n-space vertical :size="24">
      <n-card title="借阅概要">
        <n-grid :cols="6" :x-gap="12">
          <n-gi>
            <n-statistic label="总借阅量" :value="stats?.totalBorrowings ?? 0" />
          </n-gi>
          <n-gi>
            <n-statistic label="在借中" :value="stats?.activeBorrowings ?? 0" />
          </n-gi>
          <n-gi>
            <n-statistic label="逾期中">
              <span style="color: #d03050">{{ stats?.overdueBorrowings ?? 0 }}</span>
            </n-statistic>
          </n-gi>
          <n-gi>
            <n-statistic label="今日归还" :value="stats?.returnedToday ?? 0" />
          </n-gi>
          <n-gi>
            <n-statistic label="未缴罚款总额">
              ￥{{ (stats?.totalFinesAmount ?? 0).toFixed(2) }}
            </n-statistic>
          </n-gi>
          <n-gi>
            <n-statistic label="待缴罚款数" :value="stats?.unpaidFinesCount ?? 0" />
          </n-gi>
        </n-grid>
      </n-card>

      <n-card title="热门图书 Top 10">
        <n-data-table :columns="popularColumns" :data="popularBooks" :loading="popularLoading" :bordered="false" />
      </n-card>

      <n-card title="逾期报告">
        <n-data-table :columns="overdueColumns" :data="overdueList" :loading="overdueLoading" :pagination="overduePagination" remote />
      </n-card>
    </n-space>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useMessage, type DataTableColumns } from 'naive-ui'
import { api } from '@/utils/api'

interface BorrowingStatistics {
  totalBorrowings: number
  activeBorrowings: number
  overdueBorrowings: number
  returnedToday: number
  totalFinesAmount: number
  unpaidFinesCount: number
}

interface PopularBookItem {
  bookId: string
  title: string
  isbn: string
  author: string
  borrowCount: number
}

interface OverdueReportItem {
  borrowingId: string
  bookId: string
  bookTitle: string
  memberId: string
  memberName: string
  borrowDate: string
  dueDate: string
  daysOverdue: number
}

const message = useMessage()

const stats = ref<BorrowingStatistics | null>(null)
const popularBooks = ref<PopularBookItem[]>([])
const popularLoading = ref(false)
const overdueList = ref<OverdueReportItem[]>([])
const overdueLoading = ref(false)

const overduePagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    overduePagination.page = page
    fetchOverdueReport()
  },
  onUpdatePageSize: (pageSize: number) => {
    overduePagination.pageSize = pageSize
    overduePagination.page = 1
    fetchOverdueReport()
  }
})

const popularColumns: DataTableColumns<PopularBookItem> = [
  { title: '书名', key: 'title', width: 200 },
  { title: 'ISBN', key: 'isbn', width: 140 },
  { title: '作者', key: 'author', width: 140 },
  { title: '借阅次数', key: 'borrowCount', width: 100, render(row) { return `${row.borrowCount} 次` } }
]

const overdueColumns: DataTableColumns<OverdueReportItem> = [
  { title: '书名', key: 'bookTitle', width: 180 },
  { title: '借阅人', key: 'memberName', width: 120 },
  { title: '借阅日期', key: 'borrowDate', width: 110 },
  { title: '应还日期', key: 'dueDate', width: 110 },
  {
    title: '逾期天数',
    key: 'daysOverdue',
    width: 90,
    render(row) {
      return `${row.daysOverdue} 天`
    }
  }
]

async function fetchStatistics() {
  const res = await api.get<BorrowingStatistics>('/reports/statistics')
  if (res.code === 200 && res.data) {
    stats.value = res.data
  }
}

async function fetchPopularBooks() {
  popularLoading.value = true
  try {
    const res = await api.get<PopularBookItem[]>('/reports/popular-books?topN=10')
    if (res.code === 200 && res.data) {
      popularBooks.value = res.data
    }
  } catch {
    message.error('获取热门图书失败')
  } finally {
    popularLoading.value = false
  }
}

async function fetchOverdueReport() {
  overdueLoading.value = true
  try {
    const res = await api.get<{ items: OverdueReportItem[]; total: number }>(
      `/reports/overdue?page=${overduePagination.page}&pageSize=${overduePagination.pageSize}`
    )
    if (res.code === 200 && res.data) {
      overdueList.value = res.data.items
      overduePagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取逾期报表失败')
  } finally {
    overdueLoading.value = false
  }
}

onMounted(() => {
  fetchStatistics()
  fetchPopularBooks()
  fetchOverdueReport()
})
</script>
