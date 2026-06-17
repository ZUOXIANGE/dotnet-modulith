<template>
  <div class="page-container">
    <div class="page-header">
      <span>分类管理</span>
      <n-button type="primary" @click="showCreateDialog = true">新增分类</n-button>
    </div>

    <n-card>
      <n-data-table :columns="columns" :data="categories" :loading="loading" :row-key="(row: CategoryItem) => row.id" />
    </n-card>

    <n-modal v-model:show="showCreateDialog" title="新增分类" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="formRef" :model="form" :rules="rules" label-placement="left" label-width="80">
        <n-form-item label="名称" path="name">
          <n-input v-model:value="form.name" placeholder="请输入分类名称" />
        </n-form-item>
        <n-form-item label="上级分类" path="parentId">
          <n-select v-model:value="form.parentId" placeholder="无（顶级分类）" clearable :options="categoryOptions" />
        </n-form-item>
        <n-form-item label="排序" path="sortOrder">
          <n-input-number v-model:value="form.sortOrder" :min="0" :max="999" />
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

    <n-modal v-model:show="showEditDialog" title="编辑分类" preset="card" style="width: 480px" :mask-closable="false">
      <n-form ref="editFormRef" :model="editForm" :rules="rules" label-placement="left" label-width="80">
        <n-form-item label="名称" path="name">
          <n-input v-model:value="editForm.name" placeholder="请输入分类名称" />
        </n-form-item>
        <n-form-item label="上级分类" path="parentId">
          <n-select v-model:value="editForm.parentId" placeholder="无（顶级分类）" clearable :options="categoryOptions" />
        </n-form-item>
        <n-form-item label="排序" path="sortOrder">
          <n-input-number v-model:value="editForm.sortOrder" :min="0" :max="999" />
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

interface CategoryItem {
  id: string
  name: string
  description: string
  parentId: string | null
  parentName: string | null
  sortOrder: number
  createdAt: string
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

const categories = ref<CategoryItem[]>([])
const categoryOptions = ref<{ label: string; value: string }[]>([])

const columns: DataTableColumns<CategoryItem> = [
  { title: '名称', key: 'name', width: 160 },
  { title: '上级分类', key: 'parentName', width: 140, render: (row) => row.parentName ?? '-' },
  { title: '排序', key: 'sortOrder', width: 80 },
  { title: '描述', key: 'description', width: 200 },
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
  name: '',
  description: '',
  parentId: null as string | null,
  sortOrder: 0
})

const editForm = reactive({
  name: '',
  description: '',
  parentId: null as string | null,
  sortOrder: 0
})

const rules: FormRules = {
  name: [{ required: true, message: '请输入分类名称', trigger: 'blur' }]
}

async function fetchCategories() {
  loading.value = true
  try {
    const res = await api.get<CategoryItem[]>('/categories')
    if (res.code === 200 && res.data) {
      categories.value = res.data
      categoryOptions.value = res.data.map((c: CategoryItem) => ({ label: c.name, value: c.id }))
    }
  } catch {
    message.error('获取分类列表失败')
  } finally {
    loading.value = false
  }
}

function resetForm() {
  form.name = ''
  form.description = ''
  form.parentId = null
  form.sortOrder = 0
}

async function handleCreate() {
  try {
    await formRef.value?.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const res = await api.post('/categories', {
      name: form.name,
      description: form.description,
      parentId: form.parentId,
      sortOrder: form.sortOrder
    })
    if (res.code === 200) {
      message.success('新增分类成功')
      showCreateDialog.value = false
      resetForm()
      fetchCategories()
    } else {
      message.error(res.msg || '新增失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function startEdit(row: CategoryItem) {
  editingId.value = row.id
  editForm.name = row.name
  editForm.description = row.description
  editForm.parentId = row.parentId
  editForm.sortOrder = row.sortOrder
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
    const res = await api.put(`/categories/${editingId.value}`, {
      name: editForm.name,
      description: editForm.description,
      parentId: editForm.parentId,
      sortOrder: editForm.sortOrder
    })
    if (res.code === 200) {
      message.success('更新成功')
      showEditDialog.value = false
      fetchCategories()
    } else {
      message.error(res.msg || '更新失败')
    }
  } catch {
    message.error('网络错误')
  } finally {
    submitting.value = false
  }
}

function handleDelete(row: CategoryItem) {
  dialog.warning({
    title: '确认删除',
    content: `确定要删除分类"${row.name}"吗？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      const res = await api.delete(`/categories/${row.id}`)
      if (res.code === 200) {
        message.success('删除成功')
        fetchCategories()
      } else {
        message.error(res.msg || '删除失败')
      }
    }
  })
}

onMounted(() => {
  fetchCategories()
})
</script>
