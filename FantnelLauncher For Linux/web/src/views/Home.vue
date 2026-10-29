<template>
  <div class="home">
    <!-- 欢迎横幅 -->
    <div class="hero">
      <div class="hero-content">
        <h1>欢迎使用 <span class="hero-brand">Fantnel</span> 管理系统</h1>
        <p class="hero-subtitle">Fantnel 是一个功能强大的游戏服务器管理系统，提供账号管理、服务器管理、插件管理等多种功能。</p>
      </div>
    </div>

    

    <!-- 拖拽区域 -->
    <div class="drag-drop-area" @dragover.prevent @drop.prevent="handleFileDrop">
      <div class="drag-drop-content">
        <div class="drag-drop-icon">🎨</div>
        <p class="drag-drop-text">拖拽主题文件到此处</p>
        <p class="drag-drop-subtext">或点击选择文件</p>
        
        <input type="file" accept=".fant.json" class="hidden" ref="fileInput" @change="handleFileSelect">
        <button class="drag-drop-button" @click="$refs.fileInput.click()">
          选择主题文件
        </button>
      </div>
    </div>

    <Alert :show="showModal" :message="modalMessage" title="提示" @ok="reload()" />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getHome_Info, setThemeSwitch } from '../utils/Tools.js'

const showModal = ref(false)
const modalMessage = ref('')

// 广告数据
const adData = ref({
  ad1: { name: '', text: '' },
  ad2: { name: '', text: '' },
  ad3: { name: '', text: '' }
})

// 处理文件拖拽
const handleFileDrop = (event) => {
  const files = event.dataTransfer.files
  if (files.length > 0) {
    handleFile(files[0])
  }
}

// 处理文件选择
const handleFileSelect = (event) => {
  const files = event.target.files
  if (files.length > 0) {
    handleFile(files[0])
  }
}

// 处理文件
const handleFile = (file) => {
  if (file.name.endsWith('.fant.json')) {
    const reader = new FileReader()
    reader.onload = (e) => {
      try {
        showModal.value = true
        modalMessage.value = '正在应用主题，请稍后...'
        var json = JSON.parse(e.target.result)
        setThemeSwitch(json).then(data => {
          modalMessage.value = data.msg
        })
      } catch (error) {
        console.error('Fantnel 主题 解析错误:', error)
        alert('Fantnel 主题 文件解析失败，请检查文件格式。')
      }
    }
    reader.onerror = () => {
      alert('文件读取失败，请重试。')
    }
    reader.readAsText(file)
  } else {
    alert('请选择 Fantnel 主题 格式的文件。')
  }
}

const reload = () => {
  location.reload(true);
}

// 获取首页数据
onMounted(async () => {
  try {
    const data = await getHome_Info()
    adData.value = data
  } catch (error) {
    console.error('获取首页数据失败:', error)
  }
})
</script>

<style scoped>
.home {
  max-width: 1100px;
  margin: 0 auto;
  padding: 24px;
}

/* 欢迎横幅 */
.hero {
  background: linear-gradient(135deg, var(--sidebar-active-bg), var(--surface));
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  padding: 40px 36px;
  margin-bottom: 28px;
  position: relative;
  overflow: hidden;
}

.hero::after {
  content: '';
  position: absolute;
  top: -40%;
  right: -10%;
  width: 320px;
  height: 320px;
  background: radial-gradient(circle, var(--sidebar-active-bg), transparent 70%);
  pointer-events: none;
}

.hero-content {
  position: relative;
  z-index: 1;
}

.hero h1 {
  font-size: 2rem;
  font-weight: 800;
  color: var(--text-color);
  margin-bottom: 14px;
}

.hero-brand {
  background: linear-gradient(135deg, var(--sidebar-active), #22d3ee);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.hero-subtitle {
  color: var(--text-muted);
  font-size: 1.05rem;
  line-height: 1.7;
  max-width: 720px;
}

/* 广告位 */
.ads {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 18px;
  margin-bottom: 28px;
}

.ad-card {
  background-color: var(--surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  padding: 20px;
  box-shadow: 0 1px 3px var(--shadow-color);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.ad-card:hover {
  transform: translateY(-3px);
  box-shadow: 0 8px 20px var(--shadow-color);
}

.ad-card h3 {
  color: var(--text-color);
  margin-bottom: 8px;
  font-size: 1.05rem;
  font-weight: 600;
}

.ad-card p {
  color: var(--text-muted);
  line-height: 1.5;
  font-size: 0.92rem;
}

/* 拖拽区域 */
.drag-drop-area {
  background-color: var(--surface);
  border: 2px dashed var(--border-strong);
  border-radius: var(--radius-lg);
  padding: 44px 32px;
  transition: border-color 0.3s ease, background-color 0.3s ease;
}

.drag-drop-area:hover,
.drag-drop-area.dragover {
  border-color: var(--primary-color);
  background-color: var(--sidebar-active-bg);
}

.drag-drop-content {
  text-align: center;
}

.drag-drop-icon {
  font-size: 44px;
  margin-bottom: 12px;
}

.drag-drop-text {
  color: var(--text-color);
  margin-bottom: 8px;
  font-size: 1.1rem;
  font-weight: 600;
}

.drag-drop-subtext {
  color: var(--text-muted);
  font-size: 0.9rem;
  margin-bottom: 8px;
}

.drag-drop-link {
  display: inline-block;
  color: var(--primary-color);
  text-decoration: none;
  font-size: 0.9rem;
  margin-bottom: 18px;
}

.drag-drop-link:hover {
  text-decoration: underline;
}

.hidden {
  display: none;
}

.drag-drop-button {
  margin-top: 8px;
  padding: 10px 22px;
  background-color: var(--primary-color);
  color: white;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-weight: 600;
  font-size: 14px;
  transition: all 0.2s ease;
}

.drag-drop-button:hover {
  background-color: var(--primary-hover);
  transform: translateY(-1px);
  box-shadow: 0 4px 12px var(--shadow-color);
}
</style>