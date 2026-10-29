# Fantnel 登录/注册 + 收藏同步 桥接层

连接本地 MySQL，为前端提供登录/注册接口，并在登录后把服务器收藏同步到云端（未登录仍使用本地缓存）。

## 启动

```bash
cd server
npm install      # 首次安装依赖
npm start        # 启动桥接层，监听 http://localhost:3939
```

## 配置 MySQL

编辑 `server/.env`：

| 变量 | 说明 | 当前值 |
|---|---|---|
| `DB_HOST` | 数据库主机 | `localhost` |
| `DB_PORT` | 数据库端口 | `3306` |
| `DB_USER` | 数据库用户 | `fantnelp` |
| `DB_PASSWORD` | 数据库密码 | `fantnelp` |
| `DB_NAME` | 数据库名（自动创建） | `fantnel` |
| `JWT_SECRET` | 登录 token 签名密钥 | 请改为随机长字符串 |
| `BACKEND_URL` | 原有桌面后端地址 | `http://localhost:13521` |

启动后会自动：
- 连接 MySQL（若 `fantnel` 库不存在会自动创建）；
- 在 `fantnel` 库中自动创建 `users` 和 `favorites` 两张表。

> 首次运行前请先用 root 执行一次 `server/setup.sql`，给 `fantnelp` 授权
> （`GRANT ALL PRIVILEGES ON fantnel.* TO 'fantnelp'@'%'`）。

## 运行前端开发环境

```bash
npm run dev
```

`vite.config.js` 已配置代理：
- `/api/auth`、`/api/favorites` → 桥接层 `http://localhost:3939`
- 其余 `/api`、`/image` → 原后端 `http://localhost:13521`

## 生产环境

桥接层也会托管 `../dist` 前端构建产物。构建后：

```bash
npm run build   # 生成 dist
cd server && npm start   # 同时提供前端页面 + 登录注册接口 + 转发原后端
```

## 安全说明

- 所有 SQL 使用 `mysql2` 参数化占位符，**防 SQL 注入**；
- 密码使用 `bcryptjs` 加盐哈希存储，不保存明文；
- 登录/收藏接口通过 JWT `Bearer` token 鉴权。

## 客户部署（MySQL 在你本机，前端/后端在客户机）

若你的本机 MySQL 已通过公网隧道（如 80KM 穿云箭）暴露端口，客户机上的桥接层需连
你的远程 MySQL，而不是 `localhost`。

**准备（在你这台机器做一次）：**

```bash
# 以 root 创建受限远程账号（已执行过可跳过）
mysql -u root -p -e "
  CREATE USER IF NOT EXISTS 'fantnel_remote'@'%' IDENTIFIED BY '你的强密码';
  GRANT SELECT, INSERT, UPDATE, DELETE ON fantnel.users TO 'fantnel_remote'@'%';
  GRANT SELECT, INSERT, UPDATE, DELETE ON fantnel.favorites TO 'fantnel_remote'@'%';
  FLUSH PRIVILEGES;
"
```

> ⚠ **切勿把 `root` 或 `fantnelp` 全权限账号发给客户。** 只给 `fantnel_remote`
> 这类仅对两张表有增删改查权限的受限账号，并把 MySQL 绑定到所有网卡
> （`bind_address=0.0.0.0`）以便隧道转发。

**发给客户的文件：**

- `server/` 整个目录（桥接层源码）
- `dist/`（已构建好的前端，用 `npm run build` 生成）
- `customer-start.bat`

**客户操作：**

1. 把 `server\.env.customer` 的内容复制为 `server\.env`（已填好隧道地址与受限账号）
2. 在本机启动桌面后端 `Fantnel.exe`（端口 13521）
3. 双击 `customer-start.bat`（自动安装依赖、构建前端、启动桥接层）
4. 浏览器打开 `http://localhost:3939`，登录/注册在 `http://localhost:3939/site-login`

`server/.env.customer` 中把隧道入口配置为：

```env
DB_HOST=ct49uw.sky.8okm.com
DB_PORT=22293
DB_USER=fantnel_remote
DB_PASSWORD=<你的强密码>
DB_NAME=fantnel
```