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
