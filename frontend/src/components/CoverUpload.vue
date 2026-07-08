<template>
  <div class="cover-upload">
    <div class="cover-upload__preview" v-if="previewUrl">
      <img :src="previewUrl" alt="封面预览" />
      <n-button
        v-if="previewUrl"
        size="tiny"
        type="error"
        circle
        class="cover-upload__remove"
        @click="handleRemove"
      >
        ✕
      </n-button>
    </div>
    <div
      class="cover-upload__dropzone"
      :class="{ 'cover-upload__dropzone--dragover': isDragover }"
      @dragenter.prevent="onDragEnter"
      @dragover.prevent="onDragOver"
      @dragleave.prevent="onDragLeave"
      @drop.prevent="onDrop"
    >
      <input
        ref="fileInputRef"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        class="cover-upload__input"
        @change="onFileChange"
      />
      <template v-if="!uploading">
        <span class="cover-upload__icon">📷</span>
        <span class="cover-upload__text">点击或拖拽上传封面</span>
        <span class="cover-upload__hint">支持 JPG、PNG、WebP，最大 10MB</span>
      </template>
      <n-progress
        v-else
        type="circle"
        :percentage="uploadProgress"
        :status="uploadError ? 'error' : 'success'"
        style="width: 60px"
      />
    </div>
    <div class="cover-upload__error" v-if="errorMessage">{{ errorMessage }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { NButton, NProgress } from 'naive-ui'
import { uploadWithRetry } from '@/utils/api'

const MAX_SIZE = 10 * 1024 * 1024
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']

const props = defineProps<{
  coverUrl: string
  uploadId: string | null
  clearCoverImage?: boolean
}>()

const emit = defineEmits<{
  'update:coverUrl': [value: string]
  'update:uploadId': [value: string | null]
  'update:clearCoverImage': [value: boolean]
}>()

const fileInputRef = ref<HTMLInputElement | null>(null)
const uploading = ref(false)
const uploadProgress = ref(0)
const uploadError = ref(false)
const errorMessage = ref('')
const isDragover = ref(false)
const localPreview = ref('')

const previewUrl = computed(() => localPreview.value || props.coverUrl)

watch(() => props.coverUrl, () => {
  localPreview.value = ''
})

function validateFile(file: File): string | null {
  const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase()
  if (!ALLOWED_TYPES.includes(file.type) && !ALLOWED_EXTENSIONS.includes(ext)) {
    return '仅支持 JPG、PNG、WebP 格式'
  }
  if (file.size > MAX_SIZE) {
    return `文件大小不能超过 ${MAX_SIZE / 1024 / 1024}MB`
  }
  return null
}

async function uploadFile(file: File) {
  const validationError = validateFile(file)
  if (validationError) {
    errorMessage.value = validationError
    return
  }

  errorMessage.value = ''
  uploading.value = true
  uploadProgress.value = 0
  uploadError.value = false

  try {
    uploadProgress.value = 20
    const result = await uploadWithRetry(file, 'book-cover')
    uploadProgress.value = 100

    emit('update:coverUrl', '')
    emit('update:uploadId', result.uploadId)
    emit('update:clearCoverImage', false)
    localPreview.value = URL.createObjectURL(file)
  } catch (e: any) {
    uploadError.value = true
    errorMessage.value = e.message || '上传失败'
  } finally {
    setTimeout(() => {
      uploading.value = false
      uploadProgress.value = 0
      uploadError.value = false
    }, 1500)
  }
}

function handleRemove() {
  emit('update:coverUrl', '')
  emit('update:uploadId', null)
  emit('update:clearCoverImage', true)
  localPreview.value = ''
  errorMessage.value = ''
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

function onFileChange(e: Event) {
  const input = e.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) {
    uploadFile(file)
  }
}

function onDragEnter() {
  isDragover.value = true
}

function onDragOver() {
  isDragover.value = true
}

function onDragLeave() {
  isDragover.value = false
}

function onDrop(e: DragEvent) {
  isDragover.value = false
  const file = e.dataTransfer?.files?.[0]
  if (file) {
    uploadFile(file)
  }
}
</script>

<style scoped>
.cover-upload {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.cover-upload__preview {
  position: relative;
  flex-shrink: 0;
  width: 80px;
  height: 112px;
  border-radius: 4px;
  overflow: hidden;
  border: 1px solid var(--n-border-color);
  background: var(--n-color-embedded);
}

.cover-upload__preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.cover-upload__remove {
  position: absolute;
  top: 2px;
  right: 2px;
}

.cover-upload__dropzone {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  padding: 16px;
  border: 2px dashed var(--n-border-color);
  border-radius: 6px;
  cursor: pointer;
  position: relative;
  min-height: 112px;
  transition: border-color .2s, background-color .2s;
}

.cover-upload__dropzone:hover,
.cover-upload__dropzone--dragover {
  border-color: var(--n-color-primary);
  background-color: var(--n-color-primary-pressed);
}

.cover-upload__input {
  position: absolute;
  inset: 0;
  opacity: 0;
  cursor: pointer;
}

.cover-upload__icon {
  font-size: 24px;
}

.cover-upload__text {
  font-size: 13px;
  color: var(--n-text-color-2);
}

.cover-upload__hint {
  font-size: 11px;
  color: var(--n-text-color-3);
}

.cover-upload__error {
  font-size: 12px;
  color: var(--n-color-error);
  margin-top: 4px;
}
</style>
