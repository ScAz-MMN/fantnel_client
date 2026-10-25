<template>
  <div v-if="messages.length > 0" class="toast-container">
    <div 
      v-for="(msg, index) in messages" 
      :key="index"
      :class="['toast-message', `toast-${msg.type}`]"
      @transitionend="removeMessage(index)"
    >
      {{ msg.content }}
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const messages = ref([])
let messageId = 0

// 显示消息
const showMessage = (content, type = 'info', duration = 3000) => {
  const id = messageId++
  messages.value.push({
    id,
    content,
    type,
    duration
  })

  // 自动移除消息
  setTimeout(() => {
    const index = messages.value.findIndex(msg => msg.id === id)
    if (index > -1) {
      messages.value.splice(index, 1)
    }
  }, duration)
}

// 移除消息
const removeMessage = (index) => {
  // 过渡结束后移除消息
  if (!messages.value[index]) return
  messages.value.splice(index, 1)
}

// 导出消息方法
const Message = {
  success: (content, duration) => showMessage(content, 'success', duration),
  error: (content, duration) => showMessage(content, 'error', duration),
  info: (content, duration) => showMessage(content, 'info', duration),
  warning: (content, duration) => showMessage(content, 'warning', duration)
}

defineExpose({
  Message
})
</script>

<style scoped>
.toast-container {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 9999;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.toast-message {
  padding: 12px 20px;
  border-radius: var(--radius-sm);
  color: white;
  font-size: 14px;
  font-weight: 500;
  box-shadow: 0 8px 24px var(--shadow-strong);
  opacity: 0;
  transform: translateX(20px);
  animation: toastShow 0.3s cubic-bezier(0.16, 1, 0.3, 1) forwards;
  border: 1px solid rgba(255, 255, 255, 0.15);
}

.toast-success { background-color: var(--success); }
.toast-error { background-color: var(--danger-color); }
.toast-info { background-color: var(--info); }
.toast-warning { background-color: var(--warning); color: #1a1a1a; }

@keyframes toastShow {
  to {
    opacity: 1;
    transform: translateX(0);
  }
}
</style>