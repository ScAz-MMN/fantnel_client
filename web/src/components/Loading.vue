<template>
  <div v-if="visible" class="loading-overlay">
    <div class="loading-dialog">
      <div class="loading-spinner"></div>
      <p v-if="!error">{{ text }}</p>
      <p v-else>
        {{ text }}<br>
        <span v-if="countdown > 0" class="countdown-text">将在 {{ countdown }} 秒后返回...</span>
      </p>
    </div>
  </div>
</template>

<script setup>
import { watch } from 'vue'

const props = defineProps({
  visible: {
    type: Boolean,
    default: false
  },
  text: {
    type: String,
    default: '正在加载...'
  },
  error: {
    type: Boolean,
    default: false
  },
  countdown: {
    type: Number,
    default: 0
  }
})

// 监听countdown变化，实现倒计时跳转
watch(() => props.countdown, (newValue) => {
  if (newValue > 0) {
    const timer = setInterval(() => {
      // 倒计时逻辑在父组件中处理
    }, 1000)
    
    return () => clearInterval(timer)
  }
})
</script>

<style scoped>
.loading-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.loading-dialog {
  background-color: var(--surface);
  color: var(--text-color);
  padding: 32px 36px;
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
  box-shadow: 0 20px 60px var(--shadow-strong);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
  min-width: 300px;
  text-align: center;
}

.loading-spinner {
  width: 44px;
  height: 44px;
  border: 4px solid var(--surface-2);
  border-top-color: var(--sidebar-active);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }

  100% {
    transform: rotate(360deg);
  }
}

.loading-dialog p {
  margin: 0;
  font-size: 16px;
  color: var(--text-color);
  font-weight: 500;
}

.countdown-text {
  color: var(--text-muted);
  opacity: 0.8;
  font-size: 14px;
}
</style>