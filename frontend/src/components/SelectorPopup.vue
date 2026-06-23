<template>
  <div class="selector-popup">
    <n-space align="center" :size="8">
      <div class="selector-display" :class="{ 'selector-display--placeholder': !selectedLabel }">
        {{ selectedLabel || placeholder }}
      </div>
      <n-button type="primary" ghost @click="openSelector">{{ selectedLabel ? '重选' : '选择' }}</n-button>
    </n-space>
    <n-modal v-model:show="showModal" :title="title" preset="card" style="width: 700px" :mask-closable="false">
      <n-space vertical :size="12">
        <n-input
          v-model:value="searchKeyword"
          :placeholder="searchPlaceholder"
          clearable
          @keyup.enter="doSearch"
        >
          <template #suffix>
            <n-button type="primary" size="small" @click="doSearch">搜索</n-button>
          </template>
        </n-input>
        <n-data-table
          :columns="tableColumns"
          :data="items"
          :loading="loading"
          :pagination="pagination"
          :row-props="rowProps"
          remote
          size="small"
          max-height="420"
          striped
        />
      </n-space>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showModal = false">取消</n-button>
        </n-space>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, watch, h } from 'vue'
import { NButton, NSpace, NInput, NModal, NDataTable, type DataTableColumns } from 'naive-ui'
import { api } from '@/utils/api'

const props = withDefaults(defineProps<{
  modelValue: string | null
  label?: string
  placeholder?: string
  title?: string
  searchPlaceholder?: string
  apiUrl: string
  searchParam?: string
  columns: DataTableColumns<any>
  displayField: string
  rowKey?: string
  labelFormatter?: (item: any) => string
  filterFn?: (item: any) => boolean
}>(), {
  label: '',
  placeholder: '请选择',
  title: '选择',
  searchPlaceholder: '请输入关键词搜索',
  searchParam: 'keyword',
  rowKey: 'id'
})

const emit = defineEmits<{
  'update:modelValue': [value: string | null]
}>()

const showModal = ref(false)
const loading = ref(false)
const searchKeyword = ref('')
const items = ref<any[]>([])
const selectedItem = ref<any | null>(null)

const selectedLabel = computed(() => {
  if (!selectedItem.value) return ''
  if (props.labelFormatter) return props.labelFormatter(selectedItem.value)
  return selectedItem.value[props.displayField] || ''
})

const pagination = reactive({
  page: 1,
  pageSize: 10,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  prefix: ({ itemCount }: { itemCount: number | undefined }) => `共 ${itemCount} 条`,
  onChange: (page: number) => {
    pagination.page = page
    fetchItems()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchItems()
  }
})

const tableColumns = computed(() => {
  const selectCol: DataTableColumns<any> = [
    {
      title: '',
      key: 'select',
      width: 60,
      render(row) {
        return h(NButton, { size: 'tiny', type: 'primary', ghost: true, onClick: () => handleSelect(row) }, { default: () => '选择' })
      }
    }
  ]
  return [...selectCol, ...props.columns]
})

function rowProps(row: any) {
  return {
    style: 'cursor: pointer',
    onClick: () => handleSelect(row)
  }
}

function openSelector() {
  showModal.value = true
  searchKeyword.value = ''
  pagination.page = 1
  fetchItems()
}

async function fetchItems() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (searchKeyword.value) params.set(props.searchParam, searchKeyword.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: any[]; total: number }>(`${props.apiUrl}?${params.toString()}`)
    if (res.code === 200 && res.data) {
      let resultItems = res.data.items
      if (props.filterFn) resultItems = resultItems.filter(props.filterFn)
      items.value = resultItems
      pagination.itemCount = res.data.total
    }
  } catch {
    // ignore
  } finally {
    loading.value = false
  }
}

function doSearch() {
  pagination.page = 1
  fetchItems()
}

function handleSelect(row: any) {
  selectedItem.value = row
  emit('update:modelValue', row[props.rowKey])
  showModal.value = false
}

watch(() => props.modelValue, (val) => {
  if (!val) {
    selectedItem.value = null
  }
})
</script>

<style scoped>
.selector-display {
  flex: 1;
  height: 34px;
  padding: 0 12px;
  line-height: 34px;
  border: 1px solid var(--n-border-color, #d9d9d9);
  border-radius: 3px;
  background: var(--n-color, #fff);
  color: var(--n-text-color, #333);
  font-size: 14px;
  cursor: default;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  transition: border-color 0.3s;
}

.selector-display--placeholder {
  color: var(--n-placeholder-color, #bbb);
}
</style>
