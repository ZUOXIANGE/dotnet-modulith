import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

interface UserInfo {
  id: string
  userName: string
  displayName: string
  email: string
  avatarUrl: string
  permissions: string[]
  roles: string[]
}

const USER_STORAGE_KEY = 'currentUser'
const AVATAR_ACCESS_URL_KEY = 'currentUserAvatarAccessUrl'

function readStoredUser(): UserInfo | null {
  const raw = localStorage.getItem(USER_STORAGE_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as UserInfo
  } catch {
    localStorage.removeItem(USER_STORAGE_KEY)
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('accessToken'))
  const user = ref<UserInfo | null>(readStoredUser())
  const avatarAccessUrl = ref<string | null>(localStorage.getItem(AVATAR_ACCESS_URL_KEY))

  const isLoggedIn = computed(() => !!token.value)
  const userName = computed(() => user.value?.displayName ?? user.value?.userName)

  function setAuth(accessToken: string, userInfo: UserInfo) {
    token.value = accessToken
    user.value = userInfo
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(userInfo))
  }

  function setUser(userInfo: UserInfo) {
    user.value = userInfo
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(userInfo))
  }

  function setAvatarAccessUrl(value: string | null) {
    avatarAccessUrl.value = value
    if (value) {
      localStorage.setItem(AVATAR_ACCESS_URL_KEY, value)
    } else {
      localStorage.removeItem(AVATAR_ACCESS_URL_KEY)
    }
  }

  function logout() {
    token.value = null
    user.value = null
    avatarAccessUrl.value = null
    localStorage.removeItem('accessToken')
    localStorage.removeItem(USER_STORAGE_KEY)
    localStorage.removeItem(AVATAR_ACCESS_URL_KEY)
  }

  return { token, user, avatarAccessUrl, isLoggedIn, userName, setAuth, setUser, setAvatarAccessUrl, logout }
})
