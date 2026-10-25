<template>
  <div class="skins">
    <h1>皮肤管理</h1>
    <p>皮肤管理页面，用于查看和下载游戏皮肤。</p>
    <div class="search-bar">
      <input type="text" v-model="searchQuery" placeholder="搜索皮肤...">
    </div>
    <div class="skin-list" @scroll="handleScroll">
      <div v-for="skin in filteredSkins" class="skin-item" @click="navigateToSkin(skin.entity_id)">
        <div class="skin-image">
          <img :src="normalizeImage(skin.title_image_url)" alt="皮肤介绍图" width="140" height="137" loading="eager">
        </div>
        <div class="skin-info">
          <h3>{{ skin.name }}</h3>
          <p>{{ skin.brief_summary }}</p>
          <div class="skin-meta">
            <span class="meta-item">开发者: {{ skin.developer_name }}</span>
            <span class="meta-item">下载量: {{ skin.download_num }}</span>
            <span class="meta-item">点赞数: {{ skin.like_num }}</span>
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
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { getGameSkinList, getGameSkinListByName, normalizeImage } from '../../../utils/Tools'

const searchQuery = ref('')
const skins = ref([])
const searchResults = ref([])
const isSearching = ref(false)
const searchOffset = ref(0)
const isSearchComplete = ref(false)

const offset = ref(0)

// 提示框
const showNotice = ref(false)
const noticeText = ref("")
const noticeLocation = ref("")

// 获取皮肤列表
async function fetchSkins(pageSize = 20, throwError = true) {
  return getGameSkinList(offset.value, pageSize).then(res => {
    if (res.code !== 1) {
      if (throwError) {
        noticeText.value = res.msg || "获取皮肤列表失败"
        showNotice.value = true
        noticeLocation.value = "/game-accounts"
      }
      return false
    }
    if (!res || !res.data || !res.data.length) {
      return false
    }
    skins.value = [...skins.value, ...res.data]
    offset.value += pageSize
    return true
  }).catch(err => {
    noticeText.value = err.message || "获取皮肤列表异常"
    showNotice.value = true
    noticeLocation.value = "/game-accounts";
    return false;
  })
}

// 搜索皮肤
async function fetchSearchSkins(pageSize = 20, throwError = true) {
  if (!searchQuery.value.trim()) {
    return false;
  }
  
  return getGameSkinListByName(searchQuery.value, searchOffset.value, pageSize).then(res => {
    if (res.code !== 1) {
      if (throwError) {
        noticeText.value = res.msg || "搜索皮肤失败"
        showNotice.value = true
        noticeLocation.value = "/game-accounts"
      }
      return false
    }
    if (!res || !res.data || !res.data.length) {
      isSearchComplete.value = true;
      return false;
    }
    
    if (searchOffset.value === 0) {
      searchResults.value = res.data;
    } else {
      searchResults.value = [...searchResults.value, ...res.data];
    }
    
    searchOffset.value += pageSize
    return true
  }).catch(err => {
    noticeText.value = err.message || "搜索皮肤异常"
    showNotice.value = true
    noticeLocation.value = "/game-accounts";
    return false;
  })
}

const isActive = ref(true)

onMounted(() => {
  isActive.value = true
})

onUnmounted(() => {
  isActive.value = false
})

// 监听搜索词变化
watch(searchQuery, (newVal) => {
  // 重置搜索状态
  searchResults.value = []
  searchOffset.value = 0
  isSearching.value = false
  isSearchComplete.value = false
  
  if (newVal.trim()) {
    isSearching.value = true
    searchSkinsInBatches()
  }
})

// 初始加载皮肤列表
async function loadSkinsInBatches() {
  // 只有第一次加载错误时，才抛出异常
  let loading = true;
  while (true) {

    // 是否在当前页
    if (!isActive.value || isSearching.value) {
      await new Promise(resolve => setTimeout(resolve, 1000))
      continue;
    }

    const ok = await fetchSkins(15, loading)
    loading = false;
    // 如果没有更多皮肤可加载，则跳出循环
    if (!ok || offset.value >= 150) {
      break
    }
    // 每次加载完皮肤后，等待 600 毫秒
    await new Promise(resolve => setTimeout(resolve, 1200))
  }
}

// 搜索批量加载
async function searchSkinsInBatches() {
  let loading = true;
  while (true) {
    // 是否在当前页
    if (!isActive.value || !isSearching.value) {
      await new Promise(resolve => setTimeout(resolve, 1000))
      continue;
    }

    const ok = await fetchSearchSkins(15, loading)
    loading = false;
    // 如果没有更多皮肤可加载，则跳出循环
    if (!ok || searchOffset.value >= 150) {
      isSearchComplete.value = true;
      break
    }
    // 每次加载完皮肤后，等待 600 毫秒
    await new Promise(resolve => setTimeout(resolve, 1200))
  }
}

// 启动批量加载
loadSkinsInBatches()

const filteredSkins = computed(() => {
  if (searchQuery.value.trim()) {
    return searchResults.value
  } else {
    return skins.value
  }
})

function navigateToSkin(id) {
  location.href = `/skin/${id}`
}

function handleScroll(event) {
  // 可以在这里实现滚动到底部加载更多的逻辑
}

function handleNoticeOk() {
  showNotice.value = false
  if (noticeLocation.value && noticeLocation.value !== '') {
    location.href = noticeLocation.value
  }
}
</script>

<style scoped>
.skins {
  padding: 20px;
}

.search-bar {
  margin-bottom: 20px;
  margin-top: 10px;
}

.search-bar input {
  width: 100%;
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

.skin-list {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  max-height: 80vh;
  overflow-y: auto;
  padding: 10px;
}

.skin-item {
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  overflow: hidden;
  cursor: pointer;
  background: var(--surface);
  transition: transform 0.2s, box-shadow 0.2s, border-color 0.2s;
  max-height: 440px;
}

.skin-item:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 24px var(--shadow-color);
  border-color: var(--border-strong);
}

.skin-image {
  margin: 0;
  padding: 0;
  display: flex;
  justify-content: center;
  width: 100%;
  height: 60%;
  background: var(--surface-2);
}

.skin-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.skin-info {
  padding: 15px;
}

.skin-info h3 {
  margin: 0 0 10px 0;
  font-size: 18px;
  color: var(--text-color);
}

.skin-info p {
  margin: 0 0 10px 0;
  color: var(--text-muted);
  font-size: 14px;
  line-height: 1.4;
}

.skin-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  font-size: 12px;
  color: var(--text-muted);
}

.skin-meta .meta-item {
  display: flex;
  align-items: center;
  background-color: var(--surface-2);
  padding: 2px 8px;
  border-radius: 10px;
}
</style>