<template>
  <div class="game-rental">
    <h1>租赁服管理</h1>
    <p>租赁服页面，用于启动租赁服上的游戏。</p>
    <div class="search-bar">
      <input type="text" v-model="searchQuery" placeholder="搜索租赁服...">
      <button class="fav-toggle" :class="{ active: showFavoritesOnly }" @click="showFavoritesOnly = !showFavoritesOnly">
        <span class="fav-star">★</span> 收藏
      </button>
    </div>
    <div class="server-list" @scroll="handleScroll">
      <div v-for="server in filteredServers" class="server-item" @click="navigateToServer(server.entity_id)">
        <div class="server-image">
          <img :src="normalizeImage(server.image_url)" alt="服务器介绍图" width="307" height="173" loading="eager">
          <button class="fav-btn" :class="{ active: isFav(server.entity_id) }" @click.stop="handleToggleFavorite(server.entity_id)"
            :title="isFav(server.entity_id) ? '取消收藏' : '收藏'">★</button>
        </div>
        <div class="server-info">
          <h3>{{ server.server_name }}</h3>
          <p>{{ server.brief_summary }}</p>
          <div class="server-meta">
            <span class="meta-item">版本: {{ server.mc_version }}</span>
            <span class="meta-item">在线: {{ server.player_count }}/{{ server.capacity }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 使用Alert组件 -->
    <Alert :show="showNotice" :message="noticeText" title="注意事项" :location="noticeLocation" @ok="handleNoticeOk"
      @close="showNotice = false" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getRentalServerList, sortRentalServer, normalizeImage, toggleFavorite, getFavorites } from '../../../utils/Tools'

const searchQuery = ref('')
const servers = ref([])
const favoriteIds = ref(new Set(getFavorites('rentals')))
const showFavoritesOnly = ref(false)

const offset = ref(0)

// 提示框
const showNotice = ref(false)
const noticeText = ref("")
const noticeLocation = ref("")

const isActive = ref(true)

onMounted(async () => {
  isActive.value = true
  // 启动批量加载
  await sortRentalServer();
  await loadServersInBatches()
})

onUnmounted(() => {
  isActive.value = false
})

// 初始加载服务器列表
async function loadServersInBatches() {
  // 只有第一次加载错误时，才抛出异常
  let loading = true;
  while (true) {

    // 是否在当前页
    if (!isActive.value) {
      await new Promise(resolve => setTimeout(resolve, 1000))
      continue;
    }

    const ok = await loadMoreServers(15, loading)
    loading = false;
    // 如果没有更多服务器可加载，则跳出循环
    if (!ok) {
      break
    }
    // 每次加载完服务器后，等待 600 毫秒
    await new Promise(resolve => setTimeout(resolve, 700))
  }
}

const filteredServers = computed(() => {
  let list = servers.value;
  if (searchQuery.value) {
    list = list.filter(server =>
      server.server_name.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      server.brief_summary.toLowerCase().includes(searchQuery.value.toLowerCase())
    )
  }
  if (showFavoritesOnly.value) {
    list = list.filter(server => favoriteIds.value.has(String(server.entity_id)))
  }
  return list;
})

// 收藏/取消收藏
function handleToggleFavorite(id) {
  const key = String(id)
  toggleFavorite('rentals', key)
  const next = new Set(favoriteIds.value)
  if (next.has(key)) next.delete(key)
  else next.add(key)
  favoriteIds.value = next
}

function isFav(id) {
  return favoriteIds.value.has(String(id))
}

function navigateToServer(id) {
  location.href = `/game-rental/${id}`;
}

function handleScroll(event) {
  // const element = event.target;
  // if (element.scrollTop + element.clientHeight >= element.scrollHeight - 10) {
  //   // 滚动到底部，加载更多服务器
  //   loadMoreServers();
  // }
}

function loadMoreServers(pageSize = 10, throwError = true) {
  return getRentalServerList(offset.value, pageSize).then(res => {
    if (res.code !== 1) {
      if (throwError) {
        noticeText.value = res.msg || "获取租赁服列表失败"
        showNotice.value = true
        noticeLocation.value = "/game-accounts";
      }
      return false;
    }
    if (!res || !res.data || !res.data.length) {
      return false;
    }
    servers.value = [...servers.value, ...res.data]
    offset.value += pageSize
    return true;
  }).catch(err => {
    noticeText.value = err.message || "获取租赁服列表异常"
    showNotice.value = true
    noticeLocation.value = "/game-accounts";
    return false;
  })
}

function handleNoticeOk() {
  showNotice.value = false;
  if (noticeLocation.value && noticeLocation.value !== '') {
    location.href = noticeLocation.value;
  }
}
</script>

<style scoped>
.game-rental {
  padding: 20px;
}

.search-bar {
  margin-bottom: 20px;
  margin-top: 10px;
  display: flex;
  gap: 10px;
  align-items: center;
}

.search-bar input {
  flex: 1;
  padding: 12px 14px;
  border: 1px solid var(--border-strong);
  border-radius: var(--radius-sm);
  font-size: 16px;
  background: var(--surface);
  color: var(--text-color);
  transition: border-color 0.2s, box-shadow 0.2s;
}

.search-bar input:focus {
  outline: none;
  border-color: var(--sidebar-active);
  box-shadow: 0 0 0 3px var(--sidebar-active-bg);
}

.fav-toggle {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 10px 16px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  background: var(--surface);
  color: var(--text-muted);
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
  white-space: nowrap;
}

.fav-toggle .fav-star {
  font-size: 15px;
  color: var(--text-faint);
  transition: color 0.2s;
}

.fav-toggle:hover {
  border-color: var(--sidebar-active);
  color: var(--text-color);
}

.fav-toggle.active {
  background: var(--sidebar-active-bg);
  border-color: var(--sidebar-active);
  color: var(--sidebar-active);
}

.fav-toggle.active .fav-star {
  color: var(--warning);
}

.server-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
  max-height: 80vh;
  overflow-y: auto;
  padding: 10px;
}

.server-item {
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  overflow: hidden;
  cursor: pointer;
  background: var(--surface);
  transition: transform 0.2s, box-shadow 0.2s, border-color 0.2s;
}

.server-item:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 24px var(--shadow-color);
  border-color: var(--border-strong);
}

.server-image {
  position: relative;
}

.server-image img {
  width: 100%;
  height: auto;
  object-fit: cover;
  display: block;
  background: var(--surface-2);
}

.fav-btn {
  position: absolute;
  top: 10px;
  right: 10px;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 50%;
  background: rgba(0, 0, 0, 0.45);
  color: #fff;
  font-size: 17px;
  line-height: 1;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(3px);
  transition: all 0.2s;
}

.fav-btn:hover {
  transform: scale(1.12);
}

.fav-btn.active {
  color: var(--warning);
  background: rgba(0, 0, 0, 0.5);
}

.server-info {
  padding: 15px;
}

.server-info h3 {
  margin: 0 0 10px 0;
  font-size: 18px;
  color: var(--text-color);
}

.server-info p {
  margin: 0 0 10px 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.6;
}

.server-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  font-size: 12px;
  color: var(--text-muted);
}

.meta-item {
  background-color: var(--surface-2);
  padding: 3px 8px;
  border-radius: 10px;
  border: 1px solid var(--border-color);
}
</style>