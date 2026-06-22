import { useAuthStore } from '@/stores/auth'

const BASE_URL = '/api'

interface ApiResponse<T> {
  code: number
  msg: string
  data: T
}

async function request<T>(url: string, options: RequestInit = {}): Promise<ApiResponse<T>> {
  const authStore = useAuthStore()
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>)
  }

  if (authStore.token) {
    headers['Authorization'] = `Bearer ${authStore.token}`
  }

  const response = await fetch(`${BASE_URL}${url}`, {
    ...options,
    headers
  })

  if (response.status === 401) {
    authStore.logout()
    window.location.href = '/login'
    throw new Error('Unauthorized')
  }

  const body = await response.json() as ApiResponse<T>
  return body
}

export const api = {
  get<T>(url: string) {
    return request<T>(url)
  },
  post<T>(url: string, data?: unknown) {
    return request<T>(url, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined
    })
  },
  put<T>(url: string, data?: unknown) {
    return request<T>(url, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined
    })
  },
  delete<T>(url: string) {
    return request<T>(url, { method: 'DELETE' })
  }
}

export interface PresignedUploadResult {
  objectKey: string
  uploadUrl: string
  objectUrl: string
  expiresAtUtc: string
}

export async function presignCoverUpload(fileName: string): Promise<PresignedUploadResult> {
  const res = await api.post<PresignedUploadResult>('/storage/upload/presign', {
    fileName,
    objectKey: `covers/${crypto.randomUUID()}-${fileName}`
  })
  if (res.code !== 200 || !res.data) {
    throw new Error(res.msg || '获取上传地址失败')
  }
  return res.data
}

export async function uploadToPresignedUrl(uploadUrl: string, file: File): Promise<void> {
  const response = await fetch(uploadUrl, {
    method: 'PUT',
    body: file,
    headers: {
      'Content-Type': file.type || 'application/octet-stream'
    }
  })
  if (!response.ok) {
    throw new Error(`上传失败: ${response.status}`)
  }
}
