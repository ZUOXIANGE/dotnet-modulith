<template>
  <div class="login-container">
    <n-card class="login-card" title="图书馆管理系统">
      <n-form ref="formRef" :model="form" :rules="rules" label-placement="left" label-width="80">
        <n-form-item label="用户名" path="userName">
          <n-input v-model:value="form.userName" placeholder="请输入用户名" />
        </n-form-item>
        <n-form-item label="密码" path="password">
          <n-input v-model:value="form.password" type="password" placeholder="请输入密码" @keyup.enter="handleLogin" />
        </n-form-item>
        <n-form-item label="验证码" path="captchaCode">
          <n-input v-model:value="form.captchaCode" placeholder="请输入验证码" @keyup.enter="handleLogin" />
          <div class="captcha-svg" v-html="captchaSvg" @click="refreshCaptcha" title="点击刷新验证码"></div>
        </n-form-item>
        <n-form-item>
          <n-button type="primary" :loading="loading" block @click="handleLogin">登 录</n-button>
        </n-form-item>
      </n-form>
    </n-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useMessage, type FormInst, type FormRules } from 'naive-ui'
import { api } from '@/utils/api'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const message = useMessage()
const authStore = useAuthStore()
const formRef = ref<FormInst | null>(null)
const loading = ref(false)
const captchaSvg = ref('')
const captchaId = ref('')

const form = reactive({
  userName: '',
  password: '',
  captchaCode: ''
})

const rules: FormRules = {
  userName: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }],
  captchaCode: [{ required: true, message: '请输入验证码', trigger: 'blur' }]
}

async function refreshCaptcha() {
  try {
    const res = await api.get<{ captchaId: string; svgContent: string }>('/auth/captcha')
    if (res.code === 200 && res.data) {
      captchaId.value = res.data.captchaId
      captchaSvg.value = res.data.svgContent
    }
  } catch {
    message.error('获取验证码失败')
  }
}

async function handleLogin() {
  const valid = await formRef.value?.validate()
  if (!valid) return

  loading.value = true
  try {
    const res = await api.post<{
      accessToken: string
      user: {
        id: string
        userName: string
        displayName: string
        email: string
        permissions: string[]
        roles: string[]
      }
    }>('/auth/login', {
      userName: form.userName,
      password: form.password,
      captchaId: captchaId.value,
      captchaCode: form.captchaCode
    })

    if (res.code === 200 && res.data) {
      authStore.setAuth(res.data.accessToken, res.data.user)
      message.success('登录成功')
      router.push('/dashboard')
    } else {
      message.error(res.msg || '登录失败')
      refreshCaptcha()
    }
  } catch {
    message.error('网络错误，请检查后端服务是否启动')
    refreshCaptcha()
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  refreshCaptcha()
})
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.login-card {
  width: 420px;
  border-radius: 8px;
}

.captcha-svg {
  cursor: pointer;
  margin-left: 8px;
  flex-shrink: 0;
  height: 34px;
  border-radius: 4px;
  border: 1px solid #d9d9d9;
}

.captcha-svg :deep(svg) {
  display: block;
  height: 100%;
}
</style>
