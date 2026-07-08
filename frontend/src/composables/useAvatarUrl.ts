import { watch, onMounted, onUnmounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { getCurrentAvatarAccessUrl } from '@/utils/api'

/**
 * 头像预签名访问 URL 自动续期
 * 在 URL 过期前自动刷新，避免头像加载失败
 */
export function useAvatarUrl() {
  const authStore = useAuthStore()
  let timer: ReturnType<typeof setTimeout> | null = null

  async function refresh() {
    if (!authStore.token || !authStore.user?.avatarUrl) {
      authStore.setAvatarAccessUrl(null)
      return
    }

    try {
      const result = await getCurrentAvatarAccessUrl()
      authStore.setAvatarAccessUrl(result.avatarAccessUrl || null)
      scheduleRefresh(result.expiresAtUtc)
    } catch {
      authStore.setAvatarAccessUrl(null)
    }
  }

  function scheduleRefresh(expiresAtUtc?: string) {
    clearTimer()
    if (!expiresAtUtc) {
      // 无过期时间，默认 5 分钟后刷新
      timer = setTimeout(refresh, 5 * 60 * 1000)
      return
    }

    const expiresAt = new Date(expiresAtUtc).getTime()
    const now = Date.now()
    // 过期前 1 分钟刷新，最少 30 秒后执行
    const delay = Math.max(expiresAt - now - 60 * 1000, 30 * 1000)
    timer = setTimeout(refresh, delay)
  }

  function clearTimer() {
    if (timer) {
      clearTimeout(timer)
      timer = null
    }
  }

  // 头像变更（如更新头像后）触发刷新
  watch(() => authStore.user?.avatarUrl, (val) => {
    if (val) refresh()
    else authStore.setAvatarAccessUrl(null)
  })

  onMounted(() => {
    if (authStore.user?.avatarUrl) refresh()
  })

  onUnmounted(clearTimer)

  return { refresh }
}
