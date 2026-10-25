// 登录/注册 + 收藏云端同步 前端工具
// 说明：登录后 token 存 localStorage；收藏数据登录时从 MySQL 同步，
//       未登录时仍使用本地缓存（localStorage）。见 Tools.js 中的实现。

const TOKEN_KEY = 'fantnel_token'
const USER_KEY = 'fantnel_user'

/* ---------------- token / 用户 --------------- */
export function getToken() {
    return localStorage.getItem(TOKEN_KEY)
}
export function setToken(token) {
    if (token) localStorage.setItem(TOKEN_KEY, token)
    else localStorage.removeItem(TOKEN_KEY)
}
export function getUser() {
    try {
        const raw = localStorage.getItem(USER_KEY)
        return raw ? JSON.parse(raw) : null
    } catch (e) {
        return null
    }
}
export function setUser(user) {
    if (user) localStorage.setItem(USER_KEY, JSON.stringify(user))
    else localStorage.removeItem(USER_KEY)
}
export function isAuthenticated() {
    return !!getToken()
}

/* ---------------- 带 token 的请求 --------------- */
async function authFetch(url, options = {}) {
    const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) }
    const token = getToken()
    if (token) headers['Authorization'] = 'Bearer ' + token
    const res = await fetch(url, { ...options, headers })
    const data = await res.json().catch(() => ({}))
    // 401 说明 token 过期/失效，清理本地状态
    if (res.status === 401) {
        logout()
    }
    return data
}

/* ---------------- 注册 / 登录 / 当前用户 --------------- */
export async function register(username, password) {
    const data = await authFetch('/api/auth/register', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
    })
    if (data.code === 1) {
        setToken(data.data.token)
        setUser({ id: data.data.id, username: data.data.username })
    }
    return data
}

export async function login(username, password) {
    const data = await authFetch('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify({ username, password }),
    })
    if (data.code === 1) {
        setToken(data.data.token)
        setUser({ id: data.data.id, username: data.data.username })
    }
    return data
}

export async function getMe() {
    return authFetch('/api/auth/me')
}

export function logout() {
    setToken(null)
    setUser(null)
}

/* ---------------- 收藏云端同步 --------------- */
// 登录状态下，从 MySQL 拉取当前用户收藏
export async function fetchCloudFavorites() {
    if (!isAuthenticated()) return null
    const data = await authFetch('/api/favorites')
    if (data.code === 1) return data.data
    return null
}

// 登录状态下，把本地收藏推送到 MySQL（整表覆盖）
export async function pushCloudFavorites(type, ids) {
    if (!isAuthenticated()) return null
    return authFetch('/api/favorites', {
        method: 'PUT',
        body: JSON.stringify({ type, ids }),
    })
}