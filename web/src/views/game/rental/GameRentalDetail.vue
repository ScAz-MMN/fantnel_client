<template>
  <div class="server-detail">
    <div class="server-header">
      <div class="server-title">
        <h1>{{ server.name }}</h1>
        <p>服务器ID: {{ server.id }}</p>
      </div>
    </div>

    <div class="server-top">
      <div class="server-images">
        <img :src="normalizeImage(server.image_url)" alt="服务器介绍图" class="main-image" width="584" height="329" loading="eager">
      </div>

      <div class="launch-panel">
        <h2 class="panel-title">启动游戏</h2>
        
        <div class="form-group">
          <label class="panel-label">账号</label>
          <select v-model="selectedAccount" @change="selectAccount1" class="panel-select">
            <option v-for="account in accounts" :key="account.id" :value="account.id">{{ account.name }}</option>
          </select>
          
        </div>
        <div class="form-group">
          <label class="panel-label">游戏名称</label>
          <div class="select-with-add" v-if="games.length > 0">
            <select v-model="selectedGame" class="panel-select">
              <option v-for="game in games" :key="game.id" :value="game.name">{{ game.name }}</option>
            </select>
          </div>
          <div v-if="showAddGameInput" class="add-game-input">
            <input v-model="newGameName" type="text" placeholder="输入新游戏名称">
            <button type="button" class="save-game-btn" @click="addNewGame">保存</button>
          </div>
          <p class="form-hint">请先 添加 / 选择 游戏名称，才能启动游戏。</p>
        </div>
        <div class="panel-actions">
          <button type="button" class="add-game-btn" @click="showAddGameInput = !showAddGameInput">
            {{ showAddGameInput ? '取消添加' : '添加名称' }}
          </button>
          <button type="button" class="launch-game-btn" @click="launchGameBtn">启动游戏</button>
          <button type="button" @click="launchProxyBtn" class="launch-proxy-btn">启动代理</button>
        </div>
      </div>
    </div>

    <div class="server-meta">
      <div class="meta-item">
        <span class="label">游戏版本:</span>
        <span class="value">{{ server.mc_version }}</span>
      </div>
      <div class="meta-item">
        <span class="label">服务器类型:</span>
        <span class="value">{{ server.server_type }}</span>
      </div>
      <div class="meta-item">
        <span class="label">在线人数:</span>
        <span class="value">{{ server.player_count }}/{{ server.capacity }}</span>
      </div>
      <div class="meta-item">
        <span class="label">服务器地址:</span>
        <span class="value">{{ server.address }}</span>
      </div>
    </div>

    <div class="server-description">
      <h2>服务器介绍</h2>
      <p>{{ server.brief_summary }}</p>
    </div>

    <!-- 使用Alert组件 -->
    <Alert :show="showNotice" :message="noticeText" title="注意事项" :location="noticeLocation" @ok="handleNoticeOk"
      @close="showNotice = false" />
  </div>
</template>

<script setup>
import { ref, onMounted, watchEffect } from 'vue'
import { useRoute } from 'vue-router'
import { getRentalServerDetail, getRentalInfo, addRentalRole, launchGame, switchAccount, getGameAccount, launchProxy, normalizeImage } from '../../../utils/Tools'
import randomNameData from '../../../../public/random.name.json'

const route = useRoute()
const serverId = route.params.id

const server = ref({})

const selectedAccount = ref('')

const accounts = ref([])
const games = ref([])

const showAddGameInput = ref(false)
const newGameName = ref('') // 新添加的游戏名称
const selectedGame = ref('')

// 生成随机游戏名称
function generateRandomGameName() {
  const { prefixes, suffixes } = randomNameData
  let result, randomPrefix, randomSuffix, randomNumber

  do {
    randomPrefix = prefixes[Math.floor(Math.random() * prefixes.length)]
    randomSuffix = suffixes[Math.floor(Math.random() * suffixes.length)]
    randomNumber = ''

    // 先组合前缀和后缀
    let baseName = `${randomPrefix}${randomSuffix}`

    // 计算需要的随机数长度
    const currentLength = baseName.length
    if (currentLength < 7) {
      // 需要添加随机数，确保总长度在7-9之间
      const minNumbers = Math.max(0, 7 - currentLength)
      const maxNumbers = Math.max(0, 9 - currentLength)
      const numberLength = Math.floor(Math.random() * (maxNumbers - minNumbers + 1)) + minNumbers
      randomNumber = Math.floor(Math.random() * Math.pow(10, numberLength)).toString().padStart(numberLength, '0')
    } else if (currentLength > 9) {
      // 如果前缀+后缀已经超过9，需要重新选择
      continue
    }

    result = `${baseName}${randomNumber}`
  } while (result.length < 7 || result.length > 9)

  return result
}

// 当显示添加游戏输入框时自动生成随机名称
watchEffect(() => {
  if (showAddGameInput.value) {
    newGameName.value = generateRandomGameName()
  }
})

// 提示框
const showNotice = ref(false)
const noticeText = ref("")
const noticeLocation = ref("")

onMounted(() => {
  getRentalServerDetail(serverId).then(data => {
    if (data.code !== 1) {
      noticeText.value = data.msg;
      showNotice.value = true;
      noticeLocation.value = "/game-accounts";
      return;
    }
    server.value = data.data;
  })
  getServerInfo1();
})

function selectAccount1(event) {
  var account = switchAccount(event.target.value);
  account.then(data => {
    if (data.code === 1) {
      getServerInfo1();
    } else {
      noticeText.value = data.msg;
      showNotice.value = true;
      // 刷新账号选中
      refreshSelectedAccount();
    }
  });
}

function getServerInfo1() {
  getRentalInfo(serverId).then(data => {
    accounts.value = data.data.accounts
    games.value = data.data.games

    // 账号默认选中
    refreshSelectedAccount()

    // 名称默认选中
    if (games.value && games.value.length > 0) {
      selectedGame.value = games.value[0].name
    } else {
      showAddGameInput.value = true
    }
  })
}

// 刷新当前选择账号
function refreshSelectedAccount() {
  getGameAccount().then(data => {
    if (data.code === 1) {
      selectedAccount.value = data.data.id;
    }
  });
}

function launchGameBtn() {
  if (!selectedGame.value) {
    noticeText.value = '请选择游戏名称';
    showNotice.value = true;
    return;
  }
  // alert(`启动游戏: ${selectedGame.value}，账号: ${selectedAccount.value}`)
  noticeText.value = "正在启动游戏中，请稍后.....";
  launchGame(serverId, selectedGame.value, "rental").then(data => {
    // 启动游戏完成，更多信息请查看控制台
    noticeText.value = data.msg;
  }).catch(err => {
    noticeText.value = err.message;
  });
  showNotice.value = true;
}

function launchProxyBtn() {
  if (!selectedGame.value) {
    noticeText.value = '请选择游戏名称';
    showNotice.value = true;
    return;
  }
  // alert(`启动游戏: ${selectedGame.value}，账号: ${selectedAccount.value}`)
  noticeText.value = "正在启动代理中，请稍后.....";
  launchProxy(serverId, selectedGame.value, "rental").then(data => {
    // 启动代理完成，更多信息请查看控制台
    noticeText.value = data.msg;
  }).catch(err => {
    noticeText.value = err.message;
  });
  showNotice.value = true;
}

function addNewGame() {
  if (newGameName.value.trim()) {
    showNotice.value = true;
    noticeText.value = "正在添加角色中，请稍后.....";
    addRentalRole(serverId, newGameName.value).then(data => {
      if (data.code === 1) {
        getServerInfo1();
      }
      noticeText.value = data.msg;
    });
  }
}

function handleNoticeOk() {
  showNotice.value = false;
  if (noticeLocation.value && noticeLocation.value !== '') {
    location.href = noticeLocation.value;
  }
}
</script>

<style scoped>
.server-detail {
  padding: 20px;
}

.server-header {
  margin-bottom: 20px;
}

.server-title h1 {
  margin: 0;
  font-size: 24px;
  color: var(--text-color);
}

.server-title p {
  margin: 5px 0 0 0;
  color: var(--text-muted);
  font-size: 14px;
}

.server-top {
  display: flex;
  gap: 24px;
  align-items: flex-start;
  margin-bottom: 24px;
}

.server-images {
  flex: 0 0 auto;
}

.main-image {
  width: 100%;
  max-width: 584px;
  height: auto;
  object-fit: cover;
  border-radius: var(--radius-sm);
  margin-bottom: 10px;
  background: var(--surface-2);
}

.launch-panel {
  flex: 1 1 auto;
  min-width: 260px;
  padding: 20px 24px;
  background: var(--surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-color);
}

.panel-title {
  margin: 0 0 4px 0;
  font-size: 20px;
  color: var(--text-color);
}

.panel-tip {
  margin: 0 0 16px 0;
  font-size: 13px;
  color: var(--text-muted);
}

.panel-label {
  font-weight: 600;
  color: var(--text-color);
  font-size: 14px;
}

.panel-select {
  width: 100%;
  padding: 9px 12px;
  margin-top: 6px;
  border: 1px solid var(--border-color);
  border-radius: var(--radius-sm);
  background-color: var(--bg-color);
  color: var(--text-color);
  font-size: 14px;
}

.panel-select:focus {
  outline: none;
  border-color: var(--sidebar-active);
}

.form-hint {
  margin: 6px 0 0 0;
  font-size: 12px;
  color: var(--text-faint);
}

.panel-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 16px;
}

.panel-actions button {
  padding: 9px 18px;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: opacity 0.2s, transform 0.2s;
}

.panel-actions button:hover {
  opacity: 0.88;
  transform: translateY(-1px);
}

.server-meta {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 15px;
  margin-bottom: 20px;
  padding: 15px;
  background-color: var(--sidebar-bg);
  border-radius: 5px;
}

.meta-item {
  display: flex;
  flex-direction: column;
}

.meta-item .label {
  font-weight: bold;
  color: var(--text-color);
  font-size: 14px;
}

.meta-item .value {
  margin-top: 5px;
  font-size: 16px;
  color: var(--text-color);
}

.server-description {
  margin-top: 20px;
}

.server-description h2 {
  margin: 0 0 10px 0;
  font-size: 20px;
  color: var(--text-color);
}

.server-description p {
  line-height: 1.6;
  color: var(--text-color);
}

.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  backdrop-filter: blur(4px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 100;
}

.modal-content {
  background-color: var(--surface);
  color: var(--text-color);
  padding: 24px;
  border-radius: var(--radius-lg);
  width: 430px;
  border: 1px solid var(--border-color);
  box-shadow: var(--shadow-strong);
}

.form-group {
  margin-bottom: 15px;
  margin-top: 10px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  color: var(--text-color);
}

.form-group select {
  width: 100%;
  padding: 8px;
  border: 1px solid var(--border-color);
  border-radius: 3px;
  background-color: var(--bg-color);
  color: var(--text-color);
}

.form-group select:focus {
  outline: none;
  border-color: var(--sidebar-active);
}

.select-with-add {
  display: flex;
  gap: 10px;
  align-items: center;
}

.add-game-btn {
  background-color: var(--sidebar-active);
  color: white;
  border: none;
  border-radius: 3px;
}

.add-game-btn:hover {
  opacity: 0.9;
}

.add-game-input {
  display: flex;
  gap: 10px;
  margin-top: 10px;
}

.add-game-input input {
  flex: 1;
  padding: 8px;
  border: 1px solid var(--border-color);
  border-radius: 3px;
  background-color: var(--bg-color);
  color: var(--text-color);
}

.add-game-input input:focus {
  outline: none;
  border-color: var(--sidebar-active);
}

.save-game-btn {
  padding: 8px 12px;
  background-color: var(--success);
  color: white;
  border: none;
  border-radius: var(--radius-sm);
  cursor: pointer;
  font-size: 14px;
}

.save-game-btn:hover {
  opacity: 0.9;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 20px;
  gap: 10px;
}

.launch-game-btn,
.launch-proxy-btn,
.cancel-btn {
  padding: 8px 15px;
  border: none;
  border-radius: 3px;
  cursor: pointer;
}

.launch-game-btn {
  background-color: var(--success);
  color: white;
}

.launch-proxy-btn {
  background-color: var(--info);
  color: white;
}

.cancel-btn {
  background-color: var(--danger-color);
  color: white;
}
</style>