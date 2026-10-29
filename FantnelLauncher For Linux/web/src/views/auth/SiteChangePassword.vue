<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1 class="auth-title">修改密码</h1>
      <p class="auth-sub">修改登录密码，下次登录请使用新密码</p>

      <div v-if="message" class="auth-msg" :class="{ error: isError }">{{ message }}</div>

      <form @submit.prevent="doChange">
        <div class="form-group">
          <label>原密码</label>
          <input v-model="oldPassword" type="password" placeholder="请输入当前密码" autocomplete="current-password" />
        </div>
        <div class="form-group">
          <label>新密码</label>
          <input v-model="newPassword" type="password" placeholder="至少 6 位" autocomplete="new-password" />
        </div>
        <div class="form-group">
          <label>确认新密码</label>
          <input v-model="confirm" type="password" placeholder="再次输入新密码" autocomplete="new-password" />
        </div>
        <button type="submit" class="auth-btn" :disabled="loading">{{ loading ? '修改中...' : '确认修改' }}</button>
      </form>

      <p class="auth-switch">
        <router-link to="/">返回首页</router-link>
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { changePassword } from '../../utils/Auth'

const oldPassword = ref('')
const newPassword = ref('')
const confirm = ref('')
const loading = ref(false)
const message = ref('')
const isError = ref(false)

async function doChange() {
  if (!oldPassword.value || !newPassword.value) {
    message.value = '请输入原密码和新密码'
    isError.value = true
    return
  }
  if (newPassword.value !== confirm.value) {
    message.value = '两次输入的新密码不一致'
    isError.value = true
    return
  }
  loading.value = true
  message.value = ''
  isError.value = false
  const data = await changePassword(oldPassword.value, newPassword.value)
  if (data.code === 1) {
    oldPassword.value = ''
    newPassword.value = ''
    confirm.value = ''
    message.value = '密码修改成功！下次登录请使用新密码'
    // 停留本页提示，3 秒后回首页
    setTimeout(() => { location.href = '/' }, 2000)
  } else {
    message.value = data.msg || '修改失败，请重试'
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
</style>