-- ============================================================
-- Fantnel 登录/注册 数据库初始化脚本
-- 运行方式（作为 MySQL 管理员/root，本机打开 MySQL 命令行执行）：
--   mysql -u root -p < setup.sql
-- 或在 MySQL 命令行中逐条粘贴执行。
-- ============================================================

-- 1. 创建应用数据库（若不存在）
CREATE DATABASE IF NOT EXISTS fantnel
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

-- 2. 授予应用用户 fantnelp 对该数据库的全部权限
--    （如果 fantnelp 用户还没有创建，请先执行下面注释的创建用户语句）
GRANT ALL PRIVILEGES ON fantnel.* TO 'fantnelp'@'%';
FLUSH PRIVILEGES;

-- 如果 fantnelp 用户不存在，可先用 root 执行这一条（会创建用户并授权）：
-- CREATE USER IF NOT EXISTS 'fantnelp'@'%' IDENTIFIED BY 'fantnelp';
-- GRANT ALL PRIVILEGES ON fantnel.* TO 'fantnelp'@'%';
-- FLUSH PRIVILEGES;

-- 完成后重新启动桥接层：
--   cd server && npm run start
-- 启动时会自动在 fantnel 库中创建 users / favorites 两张表。