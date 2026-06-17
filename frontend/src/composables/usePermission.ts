import { useAuthStore } from '@/stores/auth'

export function usePermission() {
  const authStore = useAuthStore()

  function hasPermission(permission: string): boolean {
    return authStore.user?.permissions?.includes(permission) ?? false
  }

  function hasAnyPermission(...permissions: string[]): boolean {
    return permissions.some(p => hasPermission(p))
  }

  function hasAllPermissions(...permissions: string[]): boolean {
    return permissions.every(p => hasPermission(p))
  }

  return { hasPermission, hasAnyPermission, hasAllPermissions }
}