<script setup>
import { ref, provide, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getThemeName, setThemeName, getNirvanaAccount, initWindowMode, minimizeWindow, closeWindow, sendMessage, syncFavoritesFromCloud } from './utils/Tools'
import { getUser, isAuthenticated, logout } from './utils/Auth'

// FontAwesome 图标
import { library } from '@fortawesome/fontawesome-svg-core'
import { FontAwesomeIcon } from '@fortawesome/vue-fontawesome'
import {
  faHouse, faGamepad, faServer, faDiceD6, faPalette, faPuzzlePiece,
  faStore, faGlobe, faDesktop, faGear, faListUl, faUser,
  faMoon, faSun, faCircle, faRightToBracket, faCircleUser
} from '@fortawesome/free-solid-svg-icons'
library.add(faHouse, faGamepad, faServer, faDiceD6, faPalette, faPuzzlePiece,
  faStore, faGlobe, faDesktop, faGear, faListUl, faUser,
  faMoon, faSun, faCircle, faRightToBracket, faCircleUser)

const route = useRoute()
const router = useRouter()
const theme = ref('dark')
const account = ref('')
const siteUser = ref(getUser())
const showCloseIcon = ref(false)
const showMinimizeIcon = ref(false)
const showWindowControls = ref(false)

// 计算是否显示导航栏
const showNavbar = computed(() => {
  return route.path !== '/login-socket' && route.path !== '/site-login' && route.path !== '/site-register'
})

// Initialize theme from API
onMounted(async () => {
  initDragZone();

  const themeName = await getThemeName().then(res => res.data);
  theme.value = themeName === 'default' ? 'dark' : themeName
  // 获取涅槃账号信息
  const accountInfo = await getNirvanaAccount().then(res => res.data);
  account.value = accountInfo.account
  // 初始化消息接收
  initMessageReceiver()
  // 初始化窗口模式
  await initWindowMode();

  // 登录状态下把云端收藏拉取到本地缓存
  if (isAuthenticated()) {
    try { await syncFavoritesFromCloud() } catch (e) {}
  }
})

// 退出登录
function doLogout() {
  logout()
  siteUser.value = null
  router.push('/')
}

function toggleTheme() {
  // Calculate next theme
  const nextTheme = theme.value === 'light' ? 'dark' : theme.value === 'dark' ? 'gray' : 'light'
  // Set theme using API
  setThemeName(nextTheme)
  // Update local state
  theme.value = nextTheme
}

function handleMinimize() {
  minimizeWindow();
}

function handleClose() {
  closeWindow();
}

provide('theme', theme)

// 初始化消息接收
const initMessageReceiver = () => {
  try {
    window.external.receiveMessage(handleMessage)
  } catch (error) {
    console.error('Failed to initialize message receiver:', error)
  }
}

// 处理接收到的消息
const handleMessage = (message) => {
  try {
    var msg = JSON.parse(message)
    console.log('Received message:', msg)
    if (msg && msg.code == 5) {
      initWindow()
    }
    // 在这里处理接收到的消息
    // 例如：根据消息类型执行不同的操作
  } catch (error) {
    console.error('Failed to handle message:', error)
  }
}


// 窗口初始化
const initWindow = async () => {
  initDragZone()

  showWindowControls.value = true // 窗口控制按钮

  try {
    // 加载涅槃账号信息
    const response = await getNirvanaAccount()
    if (response.code === 1) {
      if (response.data.days < 1) {
        // 跳转到登录页面
        router.push('/login-socket')
      }
    } else {
      router.push('/login-socket')
    }
  } catch (error) {
    console.error('Failed to load nirvana account:', error)
  }

}

const initDragZone = () => {
  const isDragging = ref(false);

  document.addEventListener('mousedown', (e) => {
    if (e.target.nodeName !== 'DIV' && e.target.nodeName !== 'MAIN' && e.target.nodeName !== 'NAV') {
      return;
    }
    isDragging.value = true;
    // 记录鼠标在窗口内的偏移
    // 获取当前窗口位置
    sendMessage('window:drag-start');
  });

  document.addEventListener('mousemove', (e) => {
    if (!isDragging.value) {
      return;
    }
    // 发送绝对屏幕坐标，让后端计算
    sendMessage('window:drag-move', {
      sx: e.screenX,
      sy: e.screenY
    });
  });

  document.addEventListener('mouseup', () => {
    isDragging.value = false;
  });
}

</script>

<template>
  <div class="app" :class="theme">
    <!-- 窗口控制按钮 -->
    <div class="container" :class="{ 'full-width': !showNavbar }">
      <div class="window-controls" v-if="showWindowControls">
        <button class="window-btn minimize-btn" @click="handleMinimize" @mouseenter="showMinimizeIcon = true"
          @mouseleave="showMinimizeIcon = false">
          {{ showMinimizeIcon ? '-' : '' }}
        </button>
        <button class="window-btn close-btn" @click="handleClose" @mouseenter="showCloseIcon = true"
          @mouseleave="showCloseIcon = false">
          {{ showCloseIcon ? '×' : '' }}
        </button>
      </div>
      <!-- 左侧导航栏 -->
      <nav class="sidebar" v-if="showNavbar">
        <div class="logo">
          <h2 class="divider">Fantnel</h2>
          <router-link to="/user" class="account-link" v-if="account">
            <FontAwesomeIcon icon="fa-solid fa-user" class="nav-icon" />{{ account }}
          </router-link>
          <router-link to="/site-login" class="account-link" v-if="!siteUser && showNavbar">
            <FontAwesomeIcon icon="fa-solid fa-right-to-bracket" class="nav-icon" />登录/注册
          </router-link>
          <div class="account-link" v-if="siteUser">
            <FontAwesomeIcon icon="fa-solid fa-circle-user" class="nav-icon" />
            <span class="site-user-name" :title="siteUser.username">{{ siteUser.username }}</span>
            <button class="logout-btn" @click="doLogout" title="退出登录">退出</button>
          </div>
        </div>
        <ul class="nav-list">
          <li>
            <router-link to="/" active-class="active" class="divider">
              <FontAwesomeIcon icon="fa-solid fa-house" class="nav-icon" />主页
            </router-link>
          </li>
          <li>
            <router-link to="/game-accounts" active-class="active">
              <FontAwesomeIcon icon="fa-solid fa-gamepad" class="nav-icon" />游戏账号
            </router-link>
          </li>
          <li>
            <router-link to="/servers" active-class="active">
              <FontAwesomeIcon icon="fa-solid fa-server" class="nav-icon" />网络服
            </router-link>
          </li>
          
          <li>
            <router-link to="/skins" active-class="active" class="divider">
              <FontAwesomeIcon icon="fa-solid fa-palette" class="nav-icon" />我的皮肤
            </router-link>
          </li>
          <li>
            <router-link to="/proxy-manager" active-class="active">
              <FontAwesomeIcon icon="fa-solid fa-globe" class="nav-icon" />代理管理
            </router-link>
          </li>
          <li>
            <router-link to="/game-manager" active-class="active" class="divider">
              <FontAwesomeIcon icon="fa-solid fa-desktop" class="nav-icon" />游戏管理
            </router-link>
          </li>
          <li>
            <router-link to="/settings" active-class="active">
              <FontAwesomeIcon icon="fa-solid fa-gear" class="nav-icon" />系统设置
            </router-link>
          </li>
          <li>
            <router-link to="/logs" active-class="active">
              <FontAwesomeIcon icon="fa-solid fa-list-ul" class="nav-icon" />日志信息
            </router-link>
          </li>
        </ul>

        <!-- 主题切换 -->
        <div class="theme-switch">
          <button @click="toggleTheme" class="theme-btn" :title="theme === 'light' ? '切换到深色' : theme === 'dark' ? '切换到灰色' : '切换到浅色'">
            <FontAwesomeIcon :icon="theme === 'light' ? 'fa-solid fa-moon' : theme === 'dark' ? 'fa-solid fa-sun' : 'fa-solid fa-circle'" />
          </button>
          <span class="theme-text">{{ theme === 'light' ? '浅色' : theme === 'dark' ? '深色' : '灰色' }}</span>
        </div>
      </nav>

      <!-- 主内容区 -->
      <main class="main-content">
        <router-view />
      </main>
    </div>

    <!-- 页脚 -->
    <div class="footer" v-if="showNavbar">
      <div class="footer-content">
        <p>© 涅槃科技 2020/11/2 - 至今. 保留所有权利.</p>
        <!-- <p>备案号: 123456</p> -->
      </div>
    </div>

  </div>
</template>

<style>
/* 全局样式重置 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto,
    "PingFang SC", "Microsoft YaHei", "Helvetica Neue", Arial, sans-serif;
  transition: background-color 0.3s, color 0.3s;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

/* 自定义滚动条 */
::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}
::-webkit-scrollbar-track {
  background: transparent;
}
::-webkit-scrollbar-thumb {
  background-color: var(--border-strong);
  border-radius: 4px;
}
::-webkit-scrollbar-thumb:hover {
  background-color: var(--text-faint);
}

/* ============================================
   设计系统 - 设计令牌（Design Tokens）
   每个主题定义一组完整的语义化 CSS 变量。
   全部视图与组件统一通过 var() 引用，保证主题一致性。
   ============================================ */

/* ---------- 浅色主题 ---------- */
.app.light {
  --bg-color: #f6f8fb;
  --bg-elevated: #ffffff;
  --surface: #ffffff;
  --surface-2: #f1f4f8;
  --sidebar-bg: #ffffff;
  --sidebar-active: #2563eb;
  --sidebar-active-bg: rgba(37, 99, 235, 0.12);
  --primary-color: #2563eb;
  --primary-hover: #1d4ed8;
  --border-color: #e2e8f0;
  --border-strong: #cbd5e1;
  --ad-bg: #eef2f7;
  --text-color: #1e293b;
  --text-muted: #64748b;
  --text-faint: #94a3b8;
  --header-bg: #f1f5f9;
  --hover-color: #f1f5f9;
  --success: #16a34a;
  --success-bg: rgba(22, 163, 74, 0.12);
  --info: #2563eb;
  --info-bg: rgba(37, 99, 235, 0.12);
  --warning: #d97706;
  --warning-bg: rgba(217, 119, 6, 0.12);
  --danger-color: #dc2626;
  --danger-hover: #b91c1c;
  --danger-bg: rgba(220, 38, 38, 0.12);
  --disabled-color: #cbd5e1;
  --disabled-text: #94a3b8;
  --shadow-color: rgba(15, 23, 42, 0.08);
  --shadow-strong: rgba(15, 23, 42, 0.14);
  --radius: 10px;
  --radius-sm: 7px;
  --radius-lg: 16px;
}

/* ---------- 深色主题（默认） ---------- */
.app.dark {
  --bg-color: #0f141b;
  --bg-elevated: #1a2230;
  --surface: #171f2c;
  --surface-2: #1f2937;
  --sidebar-bg: #121a26;
  --sidebar-active: #3b82f6;
  --sidebar-active-bg: rgba(59, 130, 246, 0.16);
  --primary-color: #3b82f6;
  --primary-hover: #2563eb;
  --border-color: #2a3648;
  --border-strong: #3b4a61;
  --ad-bg: #1a2230;
  --text-color: #e6edf7;
  --text-muted: #8fa0b8;
  --text-faint: #66758c;
  --header-bg: #1c2637;
  --hover-color: #1e2a3d;
  --success: #34d399;
  --success-bg: rgba(52, 211, 153, 0.14);
  --info: #60a5fa;
  --info-bg: rgba(96, 165, 250, 0.14);
  --warning: #fbbf24;
  --warning-bg: rgba(251, 191, 36, 0.14);
  --danger-color: #f87171;
  --danger-hover: #ef4444;
  --danger-bg: rgba(248, 113, 113, 0.14);
  --disabled-color: #2f3b4e;
  --disabled-text: #66758c;
  --shadow-color: rgba(0, 0, 0, 0.4);
  --shadow-strong: rgba(0, 0, 0, 0.55);
  --radius: 10px;
  --radius-sm: 7px;
  --radius-lg: 16px;
}

/* ---------- 灰色主题 ---------- */
.app.gray {
  --bg-color: #23272e;
  --bg-elevated: #2d323b;
  --surface: #292e37;
  --surface-2: #333944;
  --sidebar-bg: #262b33;
  --sidebar-active: #3b82f6;
  --sidebar-active-bg: rgba(59, 130, 246, 0.16);
  --primary-color: #3b82f6;
  --primary-hover: #2563eb;
  --border-color: #3d4450;
  --border-strong: #4d5562;
  --ad-bg: #2d323b;
  --text-color: #cfd6e0;
  --text-muted: #8a94a3;
  --text-faint: #6b7483;
  --header-bg: #2f353f;
  --hover-color: #343b47;
  --success: #4ade80;
  --success-bg: rgba(74, 222, 128, 0.14);
  --info: #60a5fa;
  --info-bg: rgba(96, 165, 250, 0.14);
  --warning: #fbbf24;
  --warning-bg: rgba(251, 191, 36, 0.14);
  --danger-color: #f87171;
  --danger-hover: #ef4444;
  --danger-bg: rgba(248, 113, 113, 0.14);
  --disabled-color: #3d4450;
  --disabled-text: #6b7483;
  --shadow-color: rgba(0, 0, 0, 0.4);
  --shadow-strong: rgba(0, 0, 0, 0.55);
  --radius: 10px;
  --radius-sm: 7px;
  --radius-lg: 16px;
}

.app {
  background-color: var(--bg-color);
  color: var(--text-color);
  height: 100vh;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

/* 窗口控制按钮 */
.window-controls {
  position: absolute;
  top: 14px;
  right: 14px;
  display: flex;
  gap: 10px;
  z-index: 1000;
}

.window-btn {
  width: 13px;
  height: 13px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 9px;
  font-weight: bold;
  color: rgba(0, 0, 0, 0.6);
  line-height: 1;
  transition: transform 0.15s ease, filter 0.15s ease;
}

.minimize-btn {
  background-color: #4cd964;
}

.close-btn {
  background-color: #ff5f57;
}

.window-btn:hover {
  transform: scale(1.2);
  filter: brightness(1.1);
}

/* 确保容器内容不被窗口控制按钮遮挡 */
.container {
  flex: 1;
  display: flex;
  min-height: 0;
}

/* 左侧导航栏 */
.sidebar {
  width: 216px;
  background-color: var(--sidebar-bg);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
  overflow-x: hidden;
}

.sidebar .logo {
  padding: 22px 18px 18px;
  border-bottom: 1px solid var(--border-color);
  flex-shrink: 0;
}

.logo {
  padding: 0 18px 20px;
  border-bottom: 1px solid var(--border-color);
}

.logo h2 {
  font-size: 22px;
  font-weight: 800;
  letter-spacing: 0.3px;
  background: linear-gradient(135deg, var(--sidebar-active), #22d3ee);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
  margin-bottom: 4px;
}

.logo .account-link {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: var(--text-muted);
  text-decoration: none;
  padding: 7px 10px;
  margin-top: 8px;
  border-radius: var(--radius-sm);
  background-color: var(--surface-2);
  border: 1px solid var(--border-color);
  transition: all 0.2s ease;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.logo .account-link:hover {
  color: var(--sidebar-active);
  border-color: var(--sidebar-active);
}

.logo .account-link .site-user-name {
  flex: 1;
  overflow: hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
}

.logo .account-link .logout-btn {
  border: none;
  background: var(--danger-bg);
  color: var(--danger-color);
  font-size: 12px;
  padding: 3px 8px;
  border-radius: var(--radius-sm);
  cursor: pointer;
  flex-shrink: 0;
  transition: opacity 0.2s;
}

.logo .account-link .logout-btn:hover {
  opacity: 0.85;
}

.nav-list {
  list-style: none;
  flex: 1;
  padding: 10px 10px;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.nav-list li {
  margin: 0;
}

.nav-list a {
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 10px 13px;
  color: var(--text-muted);
  text-decoration: none;
  border-radius: var(--radius-sm);
  font-size: 14px;
  font-weight: 500;
  transition: all 0.18s ease;
  border: none;
}

.nav-list a .nav-icon {
  width: 20px;
  text-align: center;
  font-size: 15px;
  color: var(--text-faint);
  transition: color 0.18s ease;
  flex-shrink: 0;
}

.nav-list a:hover {
  background-color: var(--hover-color);
  color: var(--text-color);
}

.nav-list a:hover .nav-icon {
  color: var(--sidebar-active);
}

.nav-list a.active {
  background: var(--sidebar-active-bg);
  color: var(--sidebar-active);
  font-weight: 600;
}

.nav-list a.active .nav-icon {
  color: var(--sidebar-active);
}

.nav-list a.router-link-exact-active {
  background: var(--sidebar-active);
  color: #fff;
}

.nav-list a.router-link-exact-active .nav-icon {
  color: #fff;
}

/* 主题切换 */
.theme-switch {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 14px 16px;
  border-top: 1px solid var(--border-color);
  flex-shrink: 0;
  position: sticky;
  bottom: 0;
  background-color: var(--sidebar-bg);
  z-index: 2;
}

.theme-btn {
  background: var(--surface-2);
  border: 1px solid var(--border-color);
  font-size: 17px;
  cursor: pointer;
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
  padding: 0;
}

.theme-btn:hover {
  background: var(--surface);
  border-color: var(--sidebar-active);
  transform: rotate(20deg);
}

.theme-text {
  font-size: 13px;
  color: var(--text-muted);
}

/* 主内容区 */
.main-content {
  flex: 1;
  padding: 20px;
  position: relative;
  overflow-y: auto;
  height: 100%;
}

/* 当导航栏隐藏时，容器占满宽度 */
.container.full-width .main-content {
  width: 100%;
}

/* 页脚 */
.footer {
  background-color: var(--sidebar-bg);
  border-top: 1px solid var(--border-color);
  padding: 10px 20px;
  font-size: 12px;
  color: var(--text-color);
}

.footer-content {
  display: flex;
  justify-content: center;
  gap: 20px;
}
</style>
