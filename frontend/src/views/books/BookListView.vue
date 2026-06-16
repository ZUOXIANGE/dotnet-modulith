<template>
  <div class="page-container">
    <div class="page-header">
      <span>图书管理</span>
      <n-button type="primary" @click="showCreateDialog = true">新增图书</n-button>
    </div>

    <n-space vertical :size="16">
      <n-card>
        <n-space>
          <n-input v-model:value="keyword" placeholder="搜索书名/ISBN/作者" clearable style="width: 260px" @keyup.enter="search" />
          <n-select v-model:value="categoryFilter" placeholder="分类筛选" clearable :options="categoryOptions" style="width: 180px" />
          <n-button type="primary" @click="search">搜索</n-button>
        </n-space>
      </n-card>

      <n-card>
        <n-data-table :columns="columns" :data="books" :loading="loading" :pagination="pagination" remote />
      </n-card>
    </n-space>

    <n-modal v-model:show="showCreateDialog" title="新增图书" preset="card" style="width: 640px" :mask-closable="false">
      <n-form ref="formRef" :model="form" :rules="rules" label-placement="left" label-width="90">
        <n-form-item label="ISBN" path="isbn">
          <n-input v-model:value="form.isbn" placeholder="请输入ISBN" />
        </n-form-item>
        <n-form-item label="书名" path="title">
          <n-input v-model:value="form.title" placeholder="请输入书名" />
        </n-form-item>
        <n-form-item label="作者" path="author">
          <n-input v-model:value="form.author" placeholder="请输入作者" />
        </n-form-item>
        <n-form-item label="出版社" path="publisher">
          <n-input v-model:value="form.publisher" placeholder="请输入出版社" />
        </n-form-item>
        <n-form-item label="出版日期" path="publishDate">
          <n-date-picker v-model:formatted-value="form.publishDate" type="date" value-format="yyyy-MM-dd" />
        </n-form-item>
        <n-form-item label="分类" path="categoryId">
          <n-select v-model:value="form.categoryId" placeholder="请选择分类" :options="categoryOptions" />
        </n-form-item>
        <n-form-item label="总册数" path="totalCopies">
          <n-input-number v-model:value="form.totalCopies" :min="1" :max="9999" />
        </n-form-item>
        <n-form-item label="描述" path="description">
          <n-input v-model:value="form.description" type="textarea" placeholder="请输入描述" />
        </n-form-item>
      </n-form>
      <template #footer>
        <n-space justify="end">
          <n-button @click="showCreateDialog = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="handleCreate">确认</n-button>
        </n-space>
      </template>
    </n-modal>

    <n-modal v-model:show="showEditDialog" title="编辑图书" preset="card" style="width: 640px" :mask-closable="false">
      <n-form ref="editFormRef" :model="editForm" :rules="rules" label-placement="left" label-width="90">
        <n-form-item label="ISBN" path="isbn">
          <n-input v-model:value="editForm.isbn" placeholder="请输入ISBN" />
        </n-form-item>
        <n-form-item label="书名" path="title">
          <n-input v-model:value="editForm.title" placeholder="请输入书名" />
        </n-form-item>
        <n-form-item label="作者" path="author">
          <n-input v-model:value="editForm.author" placeholder="请输入作者" />
        </n-form-item>
        <n-form-item label="出版社" path="publisher">
          <n-input v-model:value="editForm.publisher" placeholder="请输入出版社" />
        </n-form-item>
        <n-form-item label="出版日期" path="publishDate">
          <n-date-picker v-model:formatted-value="editForm.publishDate" type="date" value-format="yyyy-MM-dd" />
        </n-form-item>
        <n-form-item label="分类" path="categoryId">
          <n-select v-model:value="editForm.categoryId" placeholder="请选择分类" :options="categoryOptions" />
        </n-form-item>
        <n-form-item label="总册数" path="totalCopies">
          <n-input-number v-model:value="editForm.totalCopies" :min="1" :max="9999" />
        </n-form-item>
        <n-form-item label="描述" path="description">
          <n-input v-model:value="editForm.description" type="textarea" placeholder="请输入描述" />
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
import { useMessage, useDialog, type FormInst, type FormRules, type DataTableColumns, NButton, NSpace } from 'naive-ui'
import { api } from '@/utils/api'

interface BookItem {
  id: string
  isbn: string
  title: string
  author: string
  publisher: string
  publishDate: string
  categoryName: string
  totalCopies: number
  availableCopies: number
  status: string
  createdAt: string
}

interface CategoryOption {
  label: string
  value: string
}

const message = useMessage()
const dialog = useDialog()
const loading = ref(false)
const submitting = ref(false)
const showCreateDialog = ref(false)
const showEditDialog = ref(false)
const formRef = ref<FormInst | null>(null)
const editFormRef = ref<FormInst | null>(null)
const editingId = ref<string | null>(null)

const keyword = ref('')
const categoryFilter = ref<string | null>(null)
const categoryOptions = ref<CategoryOption[]>([])
const books = ref<BookItem[]>([])
const pagination = reactive({
  page: 1,
  pageSize: 20,
  itemCount: 0,
  showSizePicker: true,
  pageSizes: [10, 20, 50],
  onChange: (page: number) => {
    pagination.page = page
    fetchBooks()
  },
  onUpdatePageSize: (pageSize: number) => {
    pagination.pageSize = pageSize
    pagination.page = 1
    fetchBooks()
  }
})

const columns: DataTableColumns<BookItem> = [
  { title: 'ISBN', key: 'isbn', width: 140 },
  { title: '书名', key: 'title', width: 200 },
  { title: '作者', key: 'author', width: 140 },
  { title: '分类', key: 'categoryName', width: 100 },
  { title: '馆藏/可借', key: 'totalCopies', width: 100, render: (row) => `${row.totalCopies}/${row.availableCopies}` },
  { title: '状态', key: 'status', width: 100 },
  {
    title: '操作',
    key: 'actions',
    width: 160,
    render(row) {
      return h(NSpace, {}, {
        default: () => [
          h(NButton, { size: 'small', onClick: () => startEdit(row) }, { default: () => '编辑' }),
          h(NButton, { size: 'small', type: 'error', onClick: () => handleDelete(row) }, { default: () => '删除' })
        ]
      })
    }
  }
]

const form = reactive({
  isbn: '',
  title: '',
  author: '',
  publisher: '',
  publishDate: '',
  categoryId: null as string | null,
  totalCopies: 1,
  description: ''
})

const editForm = reactive({
  isbn: '',
  title: '',
  author: '',
  publisher: '',
  publishDate: '',
  categoryId: null as string | null,
  totalCopies: 1,
  description: ''
})

const rules: FormRules = {
  isbn: [{ required: true, message: '请输入ISBN', trigger: 'blur' }],
  title: [{ required: true, message: '请输入书名', trigger: 'blur' }],
  author: [{ required: true, message: '请输入作者', trigger: 'blur' }],
  publisher: [{ required: true, message: '请输入出版社', trigger: 'blur' }],
  publishDate: [{ required: true, message: '请选择出版日期', trigger: 'blur' }],
  categoryId: [{ required: true, message: '请选择分类', trigger: 'blur' }],
  totalCopies: [{ required: true, type: 'number', min: 1, message: '请输入有效册数', trigger: 'blur' }]
}

async function fetchCategories() {
  const res = await api.get<CategoryOption[]>('/categories')
  if (res.code === 200 && res.data) {
    categoryOptions.value = res.data.map((c: any) => ({ label: c.name, value: c.id }))
  }
}

async function fetchBooks() {
  loading.value = true
  try {
    const params = new URLSearchParams()
    if (keyword.value) params.set('keyword', keyword.value)
    if (categoryFilter.value) params.set('categoryId', categoryFilter.value)
    params.set('page', String(pagination.page))
    params.set('pageSize', String(pagination.pageSize))

    const res = await api.get<{ items: BookItem[]; total: number }>(`/books?${params.toString()}`)
    if (res.code === 200 && res.data) {
      books.value = res.data.items
      pagination.itemCount = res.data.total
    }
  } catch {
    message.error('获取图书列表失败')
  } finally {
    loading.value = false
  }
}

function search() {
  pagination.page = 1
  fetchBooks()
}

function resetForm() {
  form.isbn = ''
  form.title = ''
  form.author = ''
  form.publisher = ''
  form.publishDate = ''
  form.categoryId = null
  form.totalCopies = 1
  form.description = ''
}

async function handleCreate() {
  const valid = await formRef.value?.validate()
  if (!valid) return

  submitting.value = true
  try {
    const res = await api.post('/books', {
      isbn: form.isbn,
      title: form.title,
      author: form.author,
      publisher: form.publisher,
      publishDate: form.publishDate,
      description: form.description,
      categoryId: form.categoryId,
      totalCopies: form.totalCopies
    })
    if (res.code === 200) {
      message.success('新增图书成功')
      showCreateDialog.value = false
      resetForm()
      fetchBooks()
    } else {
      message.error(res.msg || '新增失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function startEdit(row: BookItem) {
  editingId.value = row.id
  editForm.isbn = row.isbn
  editForm.title = row.title
  editForm.author = row.author
  editForm.publisher = row.publisher
  editForm.publishDate = row.publishDate
  editForm.categoryId = categoryOptions.value.find(c => c.label === row.categoryName)?.value ?? null
  editForm.totalCopies = row.totalCopies
  editForm.description = ''
  showEditDialog.value = true
}

async function handleUpdate() {
  const valid = await editFormRef.value?.validate()
  if (!valid || !editingId.value) return

  submitting.value = true
  try {
    const res = await api.put(`/books/${editingId.value}`, {
      isbn: editForm.isbn,
      title: editForm.title,
      author: editForm.author,
      publisher: editForm.publisher,
      publishDate: editForm.publishDate,
      description: editForm.description,
      categoryId: editForm.categoryId,
      totalCopies: editForm.totalCopies
    })
    if (res.code === 200) {
      message.success('更新成功')
      showEditDialog.value = false
      fetchBooks()
    } else {
      message.error(res.msg || '更新失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function handleDelete(row: BookItem) {
  dialog.warning({
    title: '确认删除',
    content: `确定要删除图书《${row.title}》吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.delete(`/books/${row.id}`)
      if (res.code === 200) {
        message.success('删除成功')
        fetchBooks()
      } else {
        message.error(res.msg || '删除失败')
      }
    }
  })
}

onMounted(() => {
  fetchCategories()
  fetchBooks()
})
</script>
