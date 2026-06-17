<template>
  <div class="page-container">
    <div class="page-header">工作台</div>
    <n-grid :cols="4" :x-gap="16" :y-gap="16">
      <n-grid-item>
        <n-card title="图书总量" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="stats.totalBooks" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="读者总数" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="stats.totalMembers" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="在借图书" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="stats.activeBorrowings" />
          </n-statistic>
        </n-card>
      </n-grid-item>
      <n-grid-item>
        <n-card title="逾期未还" size="small">
          <n-statistic tabular-nums>
            <n-number-animation :from="0" :to="stats.overdueBorrowings" />
          </n-statistic>
        </n-card>
      </n-grid-item>
    </n-grid>
    <n-card title="近期热门借阅" class="mt-16">
      <n-data-table v-if="popularBooks.length > 0" :columns="popularColumns" :data="popularBooks" :loading="loading" size="small" />
      <n-empty v-else-if="!loading" description="暂无数据" />
      <n-spin v-else />
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { type DataTableColumns } from 'naive-ui'
import { api } from '@/utils/api'

interface PopularBookItem {
  bookId: string
  title: string
  isbn: string
  author: string
  borrowCount: number
}

const loading = ref(false)
const stats = reactive({
  totalBooks: 0,
  totalMembers: 0,
  activeBorrowings: 0,
  overdueBorrowings: 0
})

const popularBooks = ref<PopularBookItem[]>([])

const popularColumns: DataTableColumns<PopularBookItem> = [
  { title: '书名', key: 'title', ellipsis: { tooltip: true } },
  { title: '作者', key: 'author', width: 120 },
  { title: 'ISBN', key: 'isbn', width: 140 },
  { title: '借阅次数', key: 'borrowCount', width: 100 }
]

onMounted(() => {
  fetchDashboardData()
})

async function fetchDashboardData() {
  loading.value = true
  try {
    const [booksRes, membersRes, statsRes, popularRes] = await Promise.all([
      api.get<{ total: number }>('/books?page=1&pageSize=1'),
      api.get<{ total: number }>('/members?page=1&pageSize=1'),
      api.get<{ activeBorrowings: number; overdueBorrowings: number }>('/reports/statistics'),
      api.get<PopularBookItem[]>('/reports/popular-books?topN=5')
    ])

    if (booksRes.code === 200 && booksRes.data) {
      stats.totalBooks = booksRes.data.total
    }
    if (membersRes.code === 200 && membersRes.data) {
      stats.totalMembers = membersRes.data.total
    }
    if (statsRes.code === 200 && statsRes.data) {
      stats.activeBorrowings = statsRes.data.activeBorrowings
      stats.overdueBorrowings = statsRes.data.overdueBorrowings
    }
    if (popularRes.code === 200 && popularRes.data) {
      popularBooks.value = popularRes.data
    }
  } catch {
    // dashboard data is not critical, silently ignore errors
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.mt-16 {
  margin-top: 16px;
}
</style>