import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/login/LoginView.vue'),
      meta: { title: '登录', requiresAuth: false }
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      redirect: '/dashboard',
      meta: { requiresAuth: true },
      children: [
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('@/views/dashboard/DashboardView.vue'),
          meta: { title: '工作台', requiresAuth: true }
        },
        {
          path: 'books',
          name: 'Books',
          component: () => import('@/views/books/BookListView.vue'),
          meta: { title: '图书管理', requiresAuth: true }
        },
        {
          path: 'categories',
          name: 'Categories',
          component: () => import('@/views/books/CategoryView.vue'),
          meta: { title: '分类管理', requiresAuth: true }
        },
        {
          path: 'members',
          name: 'Members',
          component: () => import('@/views/members/MemberListView.vue'),
          meta: { title: '读者管理', requiresAuth: true }
        },
        {
          path: 'borrowing',
          name: 'Borrowing',
          component: () => import('@/views/borrowing/BorrowingView.vue'),
          meta: { title: '借还管理', requiresAuth: true }
        },
        {
          path: 'reservations',
          name: 'Reservations',
          component: () => import('@/views/reservations/ReservationView.vue'),
          meta: { title: '预约管理', requiresAuth: true }
        },
        {
          path: 'fines',
          name: 'Fines',
          component: () => import('@/views/fines/FinesView.vue'),
          meta: { title: '罚款管理', requiresAuth: true }
        },
        {
          path: 'reports',
          name: 'Reports',
          component: () => import('@/views/reports/ReportsView.vue'),
          meta: { title: '统计报表', requiresAuth: true }
        },
        {
          path: 'users',
          name: 'Users',
          component: () => import('@/views/users/UserListView.vue'),
          meta: { title: '用户管理', requiresAuth: true }
        },
        {
          path: 'roles',
          name: 'Roles',
          component: () => import('@/views/users/RoleView.vue'),
          meta: { title: '角色管理', requiresAuth: true }
        }
      ]
    }
  ]
})

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore()

  if (to.path === '/login') {
    if (authStore.isLoggedIn) {
      next('/dashboard')
    } else {
      next()
    }
    return
  }

  if (to.meta.requiresAuth && !authStore.isLoggedIn) {
    next('/login')
    return
  }

  next()
})

export default router