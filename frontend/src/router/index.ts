import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/views/login/LoginView.vue'),
      meta: { title: '登录' }
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          name: 'Dashboard',
          component: () => import('@/views/dashboard/DashboardView.vue'),
          meta: { title: '工作台' }
        },
        {
          path: 'books',
          name: 'Books',
          component: () => import('@/views/books/BookListView.vue'),
          meta: { title: '图书管理' }
        },
        {
          path: 'categories',
          name: 'Categories',
          component: () => import('@/views/books/CategoryView.vue'),
          meta: { title: '分类管理' }
        },
        {
          path: 'members',
          name: 'Members',
          component: () => import('@/views/members/MemberListView.vue'),
          meta: { title: '读者管理' }
        },
        {
          path: 'borrowing',
          name: 'Borrowing',
          component: () => import('@/views/borrowing/BorrowingView.vue'),
          meta: { title: '借还管理' }
        },
        {
          path: 'reservations',
          name: 'Reservations',
          component: () => import('@/views/reservations/ReservationView.vue'),
          meta: { title: '预约管理' }
        },
        {
          path: 'fines',
          name: 'Fines',
          component: () => import('@/views/fines/FinesView.vue'),
          meta: { title: '罚款管理' }
        },
        {
          path: 'reports',
          name: 'Reports',
          component: () => import('@/views/reports/ReportsView.vue'),
          meta: { title: '统计报表' }
        },
        {
          path: 'users',
          name: 'Users',
          component: () => import('@/views/users/UserListView.vue'),
          meta: { title: '用户管理' }
        },
        {
          path: 'roles',
          name: 'Roles',
          component: () => import('@/views/users/RoleView.vue'),
          meta: { title: '角色管理' }
        }
      ]
    }
  ]
})

export default router