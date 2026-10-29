<template>
  <div v-if="show" class="alert-notice">
    <div class="alert-content">
      <div class="alert-header">
        <strong v-if="title" v-html="title"></strong>
        <strong v-else>提示</strong>
        <button v-if="closable" class="alert-close" @click="handleClose">&times;</button>
      </div>
      <div class="alert-body">
        <ul v-if="Array.isArray(message)">
          <li v-for="(item, index) in message" :key="index">{{ item }}</li>
        </ul>
        <div v-else v-html="message"></div>
      </div>
      <div class="alert-footer">
        <button v-if="showCancel" class="alert-cancel" @click="handleCancel">{{ cancelText }}</button>
        <button class="alert-ok" @click="handleOk">{{ okText }}</button>
      </div>
    </div>
  </div>
</template>

<script setup>

// Props
const props = defineProps({
  show: {
    type: Boolean,
    default: false
  },
  message: {
    type: [String, Array],
    default: ''
  },
  title: {
    type: String,
    default: '提示'
  },
  closable: {
    type: Boolean,
    default: true
  },
  showCancel: {
    type: Boolean,
    default: false
  },
  okText: {
    type: String,
    default: '我知道了'
  },
  cancelText: {
    type: String,
    default: '取消'
  },
  location: {
    type: String,
    default: ''
  }
});

// Emits
const emit = defineEmits(['close', 'ok', 'cancel']);

// Methods
function handleClose() {
  emit('close');
}

function handleOk() {
  emit('ok');
  if (props.location) {
    location.href = props.location;
  }
}

function handleCancel() {
  emit('cancel');
}
</script>

<style scoped>
.alert-notice {
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
  animation: fadeIn 0.2s ease;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.alert-content {
  background-color: var(--surface);
  color: var(--text-color);
  border-radius: var(--radius);
  box-shadow: 0 20px 60px var(--shadow-strong);
  width: 400px;
  max-width: 90%;
  border: 1px solid var(--border-color);
  animation: scaleIn 0.22s cubic-bezier(0.16, 1, 0.3, 1);
}

@keyframes scaleIn {
  from { transform: scale(0.94); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

.alert-header {
  padding: 18px 22px;
  border-bottom: 1px solid var(--border-color);
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.alert-header strong {
  font-size: 16px;
  font-weight: 600;
}

.alert-close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  color: var(--text-muted);
  padding: 0;
  width: 28px;
  height: 28px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-sm);
  transition: all 0.2s;
}

.alert-close:hover {
  background-color: var(--hover-color);
  color: var(--text-color);
}

.alert-body {
  padding: 22px;
  font-size: 0.95rem;
  line-height: 1.6;
  color: var(--text-muted);
}

.alert-body ul {
  margin: 0;
  padding-left: 20px;
}

.alert-body li {
  margin: 8px 0;
  line-height: 1.4;
}

.alert-footer {
  padding: 14px 22px;
  border-top: 1px solid var(--border-color);
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

.alert-ok {
  padding: 9px 20px;
  background-color: var(--primary-color);
  color: white;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: background-color 0.2s;
}

.alert-ok:hover {
  background-color: var(--primary-hover);
}

.alert-cancel {
  padding: 9px 20px;
  background-color: var(--surface-2);
  color: var(--text-color);
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}

.alert-cancel:hover {
  background-color: var(--hover-color);
}
</style>