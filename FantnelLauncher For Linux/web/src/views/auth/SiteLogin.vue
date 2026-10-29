<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1 class="auth-title">登录</h1>
      <p class="auth-sub">登录后你的服务器收藏将同步到云端</p>

      <div v-if="message" class="auth-msg" :class="{ error: isError }">{{ message }}</div>

      <form @submit.prevent="doLogin">
        <div class="form-group">
          <label>用户名</label>
          <input v-model.trim="username" type="text" placeholder="请输入用户名" autocomplete="username" />
        </div>
        <div class="form-group">
          <label>密码</label>
          <input v-model="password" type="password" placeholder="请输入密码" autocomplete="current-password" />
        </div>
        <button type="submit" class="auth-btn" :disabled="loading">{{ loading ? '登录中...' : '登录' }}</button>
      </form>

      <p class="auth-switch">
        还没有账号？<router-link to="/site-register">立即注册</router-link>
      </p>
      <p class="auth-back">
        <router-link to="/">返回首页</router-link>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { login } from '../../utils/Auth'
import { syncFavoritesFromCloud } from '../../utils/Tools'

const username = ref('')
const password = ref('')
const loading = ref(false)
const message = ref('')
const isError = ref(false)

async function doLogin() {
  if (!username.value || !password.value) {
    message.value = '请输入用户名和密码'
    isError.value = true
    return
  }
  loading.value = true
  message.value = ''
  isError.value = false
  const data = await login(username.value, password.value)
  if (data.code === 1) {
    // 登录后把云端收藏拉取到本地缓存
    try { await syncFavoritesFromCloud() } catch (e) {}
    message.value = '登录成功，正在跳转...'
    // 跳转到首页并整页刷新，让顶部登录状态与收藏同步生效
    setTimeout(() => { location.href = '/' }, 800)
  } else {
    message.value = data.msg || '登录失败，请重试'
    isError.value = true
  }
  loading.value = false
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}
.auth-card {
  width: 100%;
  max-width: 380px;
  padding: 36px 32px;
  background: var(--surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-strong);
}
.auth-title {
  margin: 0;
  font-size: 26px;
  color: var(--text-color);
}
.auth-sub {
  margin: 8px 0 20px;
  font-size: 14px;
  color: var(--text-muted);
}
.auth-msg {
  padding: 10px 12px;
  margin-bottom: 16px;
  border-radius: var(--radius-sm);
  font-size: 14px;
  background: var(--success-bg);
  color: var(--success);
}
.auth-msg.error {
  background: var(--danger-bg);
  color: var(--danger-color);
}
.form-group {
  margin-bottom: 16px;
}
.form-group label {
  display: block;
  margin-bottom: 6px;
  font-size: 14px;
  color: var(--text-color);
}
.form-group input {
  width: 100%;
  padding: 12px 14px;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  background: var(--bg-color);
  color: var(--text-color);
  font-size: 15px;
  box-sizing: border-box;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-group input:focus {
  outline: none;
  border-color: var(--sidebar-active);
  box-shadow: 0 0 0 3px var(--sidebar-active-bg);
}
.auth-btn {
  width: 100%;
  padding: 12px;
  border: none;
  border-radius: var(--radius-sm);
  background: var(--primary-color);
  color: #fff;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: opacity 0.2s;
}
.auth-btn:hover:not(:disabled) {
  opacity: 0.9;
}
.auth-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.auth-switch {
  margin: 20px 0 0;
  text-align: center;
  font-size: 14px;
  color: var(--text-muted);
}
.auth-switch a {
  color: var(--primary-color);
  text-decoration: none;
  font-weight: 600;
}
.auth-switch a:hover {
  text-decoration: underline;
}
.auth-back {
  margin: 12px 0 0;
  text-align: center;
  font-size: 13px;
}
.auth-back a {
  color: var(--text-faint);
  text-decoration: none;
}
.auth-back a:hover {
  color: var(--text-muted);
}
</style>