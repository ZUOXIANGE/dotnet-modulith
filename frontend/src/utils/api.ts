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

export interface UploadSessionResult {
  uploadId: string
  objectKey: string
  uploadUrl: string
  expiresAtUtc: string
}

export async function createUploadSession(
  fileName: string,
  contentType: string,
  purpose: 'book-cover' | 'user-avatar'
): Promise<UploadSessionResult> {
  const res = await api.post<UploadSessionResult>('/storage/upload/presign', {
    fileName,
    contentType,
    purpose
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

/**
 * 带重试与重新签名的上传降级
 * presigned PUT 失败时，重新申请签名并重试，覆盖 URL 过期/临时网络错误
 */
export async function uploadWithRetry(
  file: File,
  purpose: 'book-cover' | 'user-avatar',
  maxAttempts = 2
): Promise<UploadSessionResult> {
  let lastError: Error | null = null
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    try {
      const session = await createUploadSession(file.name, file.type || 'application/octet-stream', purpose)
      await uploadToPresignedUrl(session.uploadUrl, file)
      return session
    } catch (e) {
      lastError = e instanceof Error ? e : new Error(String(e))
      if (attempt === maxAttempts - 1) break
      await new Promise(r => setTimeout(r, 500))
    }
  }
  throw lastError ?? new Error('上传失败')
}

export async function setCurrentAvatar(uploadId: string) {
  const res = await api.put<{
    id: string
    userName: string
    displayName: string
    email: string
    avatarUrl: string
    permissions: string[]
    roles: string[]
  }>('/auth/avatar', { uploadId })

  if (res.code !== 200 || !res.data) {
    throw new Error(res.msg || '设置头像失败')
  }

  return res.data
}

export async function getCurrentUser() {
  const res = await api.get<{
    id: string
    userName: string
    displayName: string
    email: string
    avatarUrl: string
    permissions: string[]
    roles: string[]
  }>('/auth/me')

  if (res.code !== 200 || !res.data) {
    throw new Error(res.msg || '获取当前用户失败')
  }

  return res.data
}

export async function getCurrentAvatarAccessUrl() {
  const res = await api.get<{
    avatarAccessUrl: string
    expiresAtUtc: string
  }>('/auth/avatar-access-url')

  if (res.code !== 200 || !res.data) {
    throw new Error(res.msg || '获取头像访问地址失败')
  }

  return res.data
}
