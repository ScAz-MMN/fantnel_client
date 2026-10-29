/**
 * Fantnel 登录/注册 + 收藏同步 桥接层
 *
 * 作用：
 *  1. 连接 MySQL，存储用户名/密码(带盐哈希)与用户收藏数据
 *  2. 提供 /api/auth/* 与 /api/favorites/* HTTP 接口（预编译 SQL 防注入）
 *  3. 把其余 /api/* 与 /image/* 转发到原有桌面端后端 (BACKEND_URL)
 *  4. 托管前端构建产物 (../web/dist)，若存在
 *
 * 安全要点：
 *  - 全部 SQL 使用 mysql2 参数化占位符(?) —— 用户输入永不拼接进 SQL 字符串，防 SQL 注入
 *  - 密码使用 bcryptjs 加盐哈希，绝不存明文
 *  - JWT 鉴权保护 /api/auth/me 与 /api/favorites/*
 */
import "dotenv/config";
import dotenv from "dotenv";
import express from "express";
import cors from "cors";
import mysql from "mysql2/promise";
import bcrypt from "bcryptjs";
import jwt from "jsonwebtoken";
import { createProxyMiddleware } from "http-proxy-middleware";
import path from "node:path";
import fs from "node:fs";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// 无论从哪个目录启动，都加载本文件所在目录的 .env
dotenv.config({ path: path.join(__dirname, ".env") });

const PORT = Number(process.env.PORT) || 3939;
const BACKEND_URL = process.env.BACKEND_URL || "http://localhost:13521";
const JWT_SECRET = process.env.JWT_SECRET || "fantnel_default_secret";
const DB = {
  host: process.env.DB_HOST || "localhost",
  port: Number(process.env.DB_PORT) || 3306,
  user: process.env.DB_USER || "root",
  password: process.env.DB_PASSWORD || "",
  database: process.env.DB_NAME || "fantnel",
};

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

/* ------------------------------------------------------------------ */
/* MySQL 连接池                                                         */
/* ------------------------------------------------------------------ */
const pool = mysql.createPool({
  host: DB.host,
  port: DB.port,
  user: DB.user,
  password: DB.password,
  database: DB.database,
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0,
  charset: "utf8mb4",
});

/** 建表（users 表 + favorites 表），启动时自动执行。
 *  用受限远程用户时可能无 CREATE 权限，权限错误仅警告、不阻断（表通常已存在）。 */
async function initSchema() {
  const conn = await pool.getConnection();
  try {
    for (const ddl of [
      `CREATE TABLE IF NOT EXISTS users (
        id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        username VARCHAR(64) NOT NULL UNIQUE,
        password_hash VARCHAR(255) NOT NULL,
        created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4`,
      `CREATE TABLE IF NOT EXISTS favorites (
        id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
        user_id INT UNSIGNED NOT NULL,
        type VARCHAR(16) NOT NULL,
        item_id VARCHAR(64) NOT NULL,
        UNIQUE KEY uq_user_type_item (user_id, type, item_id),
        CONSTRAINT fk_fav_user FOREIGN KEY (user_id)
          REFERENCES users(id) ON DELETE CASCADE
      ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4`,
    ]) {
      try {
        await conn.query(ddl);
      } catch (e) {
        console.warn(`[mysql] 建表跳过（可能已存在或无权限）：${e.message}`);
      }
    }
  } finally {
    conn.release();
  }
}

/** 若数据库不存在则自动创建。无 CREATE 权限时仅警告、不阻断。 */
async function ensureDatabase() {
  const c = await mysql.createConnection({
    host: DB.host,
    port: DB.port,
    user: DB.user,
    password: DB.password,
  });
  try {
    await c.query(
      `CREATE DATABASE IF NOT EXISTS \`${DB.database}\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci`
    );
  } finally {
    await c.end();
  }
}

/** 校验用户名/密码规则 */
function validateCredentials(username, password) {
  if (!username || typeof username !== "string") return "用户名不能为空";
  username = username.trim();
  if (username.length < 3 || username.length > 64) return "用户名长度需在 3-64 位之间";
  if (!/^[A-Za-z0-9_\u4e00-\u9fa5]+$/.test(username)) return "用户名只能包含字母、数字、下划线或中文";
  if (!password || typeof password !== "string") return "密码不能为空";
  if (password.length < 6 || password.length > 128) return "密码长度需在 6-128 位之间";
  return null;
}

/* ------------------------------------------------------------------ */
/* 鉴权中间件：从 Authorization: Bearer <token> 读取并校验 JWT           */
/* ------------------------------------------------------------------ */
function authRequired(req, res, next) {
  const header = req.headers.authorization || "";
  const token = header.startsWith("Bearer ") ? header.slice(7) : null;
  if (!token) return res.status(401).json({ code: 401, msg: "未登录或登录已过期" });
  try {
    const payload = jwt.verify(token, JWT_SECRET);
    req.user = { id: payload.id, username: payload.username };
    next();
  } catch (e) {
    return res.status(401).json({ code: 401, msg: "登录已过期，请重新登录" });
  }
}

/* ------------------------------------------------------------------ */
/* 注册                                                                 */
/* ------------------------------------------------------------------ */
app.post("/api/auth/register", async (req, res) => {
  let { username, password } = req.body || {};
  const err = validateCredentials(username, password);
  if (err) return res.status(400).json({ code: 400, msg: err });
  username = username.trim();

  try {
    // 1. 检查用户名是否已存在
    const [existing] = await pool.query(
      "SELECT id FROM users WHERE username = ? LIMIT 1",
      [username]
    );
    if (existing.length > 0) {
      return res.status(400).json({ code: 400, msg: "用户名已被注册" });
    }

    // 2. 加盐哈希密码
    const passwordHash = await bcrypt.hash(password, 10);

    // 3. 插入新用户（预编译参数化）
    const [result] = await pool.query(
      "INSERT INTO users (username, password_hash) VALUES (?, ?)",
      [username, passwordHash]
    );
    const userId = result.insertId;

    // 4. 直接签发 token，注册后免登录
    const token = jwt.sign({ id: userId, username }, JWT_SECRET, { expiresIn: "7d" });
    res.json({ code: 1, msg: "注册成功", data: { id: userId, username, token } });
  } catch (e) {
    console.error("[register] error:", e.message);
    res.status(500).json({ code: 500, msg: dbErrMsg(e) });
  }
});

/* ------------------------------------------------------------------ */
/* 登录                                                                 */
/* ------------------------------------------------------------------ */
app.post("/api/auth/login", async (req, res) => {
  const { username, password } = req.body || {};
  if (!username || !password) {
    return res.status(400).json({ code: 400, msg: "请输入用户名和密码" });
  }
  try {
    const [rows] = await pool.query(
      "SELECT id, username, password_hash FROM users WHERE username = ? LIMIT 1",
      [username.trim()]
    );
    if (rows.length === 0) {
      return res.status(400).json({ code: 400, msg: "用户名或密码错误" });
    }
    const user = rows[0];
    const ok = await bcrypt.compare(password, user.password_hash);
    if (!ok) {
      return res.status(400).json({ code: 400, msg: "用户名或密码错误" });
    }
    const token = jwt.sign({ id: user.id, username: user.username }, JWT_SECRET, {
      expiresIn: "7d",
    });
    res.json({ code: 1, msg: "登录成功", data: { id: user.id, username: user.username, token } });
  } catch (e) {
    console.error("[login] error:", e.message);
    res.status(500).json({ code: 500, msg: dbErrMsg(e) });
  }
});

/* ------------------------------------------------------------------ */
/* 当前登录用户                                                        */
/* ------------------------------------------------------------------ */
app.get("/api/auth/me", authRequired, (req, res) => {
  res.json({ code: 1, data: { id: req.user.id, username: req.user.username } });
});

/* ------------------------------------------------------------------ */
/* 收藏：查询当前用户收藏                                               */
/* ------------------------------------------------------------------ */
app.get("/api/favorites", authRequired, async (req, res) => {
  try {
    const [rows] = await pool.query(
      "SELECT type, item_id FROM favorites WHERE user_id = ?",
      [req.user.id]
    );
    const result = { servers: [], rentals: [] };
    for (const r of rows) {
      if (!result[r.type]) result[r.type] = [];
      result[r.type].push(r.item_id);
    }
    res.json({ code: 1, data: result });
  } catch (e) {
    console.error("[favorites:get] error:", e.message);
    res.status(500).json({ code: 500, msg: dbErrMsg(e) });
  }
});

/* ------------------------------------------------------------------ */
/* 收藏：整表覆盖写入（body: { type: 'servers'|'rentals', ids: [] }）    */
/* 事务 + 参数化，防注入                                                */
/* ------------------------------------------------------------------ */
app.put("/api/favorites", authRequired, async (req, res) => {
  const { type, ids } = req.body || {};
  if (type !== "servers" && type !== "rentals") {
    return res.status(400).json({ code: 400, msg: "收藏类型不合法" });
  }
  const list = Array.isArray(ids) ? ids.filter((x) => x != null).map(String) : [];
  const conn = await pool.getConnection();
  try {
    await conn.beginTransaction();
    await conn.query("DELETE FROM favorites WHERE user_id = ? AND type = ?", [
      req.user.id,
      type,
    ]);
    if (list.length > 0) {
      const values = list.map((item) => [req.user.id, type, item]);
      await conn.query(
        "INSERT INTO favorites (user_id, type, item_id) VALUES ?",
        [values]
      );
    }
    await conn.commit();
    res.json({ code: 1, msg: "收藏已同步", data: list });
  } catch (e) {
    await conn.rollback();
    console.error("[favorites:put] error:", e.message);
    res.status(500).json({ code: 500, msg: dbErrMsg(e) });
  } finally {
    conn.release();
  }
});

/* ------------------------------------------------------------------ */
/* 数据库连接失败的友好提示                                              */
/* ------------------------------------------------------------------ */
function dbErrMsg(e) {
  if (e && e.code === "ECONNREFUSED") {
    return "数据库连接失败：请确认本地 MySQL 已启动并正确填写 server/.env 配置";
  }
  if (e && e.code === "ER_ACCESS_DENIED_ERROR") {
    return "数据库连接失败：用户名或密码错误";
  }
  if (e && e.code === "ER_BAD_DB_ERROR") {
    return "数据库连接失败：数据库不存在（请在 MySQL 中创建，或修改 server/.env 的 DB_NAME）";
  }
  return "服务异常，请稍后重试";
}

/* ------------------------------------------------------------------ */
/* 启动时初始化数据库与表结构                                             */
/* ------------------------------------------------------------------ */
ensureDatabase()
  .catch((e) => {
    // 无 CREATE 权限（受限远程用户）或库已存在时，仅警告，不阻断
    console.warn(`[mysql] 自动建库跳过：${e.message}`);
  })
  .then(() => initSchema())
  .then(() => {
    console.log(`[mysql] 已连接 ${DB.host}:${DB.port}/${DB.database}，数据库与表结构就绪`);
  })
  .catch((e) => {
    // 数据库初始化失败不阻断启动，接口会返回友好错误；打印提示即可
    console.warn(`[mysql] 初始化数据库失败：${e.message}（登录/注册接口将暂不可用）`);
  });

/* ------------------------------------------------------------------ */
/* 转发：把除 auth/favorites 外的 /api 与 /image 转发到原桌面后端          */
/* ------------------------------------------------------------------ */
const proxy = createProxyMiddleware({
  target: BACKEND_URL,
  changeOrigin: true,
  logLevel: "warn",
});

app.use("/image", proxy);
app.use((req, res, next) => {
  // 由桥接层自己处理的路径，跳过转发
  if (req.path.startsWith("/api/auth") || req.path.startsWith("/api/favorites")) {
    return next();
  }
  // 其余 /api 转发到原后端
  if (req.path.startsWith("/api")) return proxy(req, res, next);
  return next();
});

/* ------------------------------------------------------------------ */
/* 静态托管前端构建产物（若存在 web/dist）                               */
/* ------------------------------------------------------------------ */
const distPath = path.resolve(__dirname, "../dist");
if (fs.existsSync(distPath)) {
  app.use(express.static(distPath));
  app.get(/.*/, (req, res, next) => {
    // 非 /api 与 /image 的请求回退到 index.html（支持前端 history 路由）
    if (req.path.startsWith("/api") || req.path.startsWith("/image")) return next();
    res.sendFile(path.join(distPath, "index.html"));
  });
}

app.listen(PORT, () => {
  console.log(`[server] Fantnel 桥接层已启动: http://localhost:${PORT}`);
  console.log(`[server] 转发原后端: ${BACKEND_URL}`);
  console.log(`[server] 请在 server/.env 中配置本地 MySQL（当前 ${DB.host}:${DB.port}/${DB.database}）`);
});