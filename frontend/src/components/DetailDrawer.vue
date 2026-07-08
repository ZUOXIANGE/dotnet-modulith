<template>
  <n-drawer :show="show" :width="480" placement="right" @update:show="$emit('update:show', $event)">
    <n-drawer-content :title="title" closable>
      <n-spin :show="loading">
        <n-descriptions label-placement="left" bordered :column="1" size="small">
          <n-descriptions-item v-for="field in fields" :key="field.label" :label="field.label">
            <template v-if="field.render">
              <component :is="field.render" />
            </template>
            <template v-else>
              {{ formatValue(field.value) }}
            </template>
          </n-descriptions-item>
        </n-descriptions>
      </n-spin>
    </n-drawer-content>
  </n-drawer>
</template>

<script setup lang="ts">
import { NDrawer, NDrawerContent, NSpin, NDescriptions, NDescriptionsItem } from 'naive-ui'
import type { VNode } from 'vue'

export interface DetailField {
  label: string
  value?: unknown
  render?: VNode
}

defineProps<{
  show: boolean
  title: string
  loading?: boolean
  fields: DetailField[]
}>()

defineEmits<{
  'update:show': [value: boolean]
}>()

function formatValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return '-'
  if (typeof value === 'boolean') return value ? '是' : '否'
  if (typeof value === 'number') return String(value)
  if (typeof value === 'string') {
    // ISO 日期字符串简单格式化
    if (/^\d{4}-\d{2}-\d{2}T/.test(value)) {
      const d = new Date(value)
      return isNaN(d.getTime()) ? value : d.toLocaleString()
    }
    return value
  }
  return String(value)
}
</script>
