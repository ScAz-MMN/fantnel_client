@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
title Fantnel 客户版一键启动
cd /d "%~dp0"
set "BRIDGE_DIR=%~dp0server"

echo ============================================
echo    Fantnel 客户版一键启动
echo ============================================
echo.

REM ---------- 检查 Node.js ----------
where node >nul 2>nul
if %errorlevel% neq 0 (
  echo [错误] 未检测到 Node.js，请先安装: https://nodejs.org
  echo.
  pause
  exit /b 1
)

REM ---------- 检查客户 .env 是否存在 ----------
if not exist "%BRIDGE_DIR%\.env" (
  echo [错误] 缺少 server\.env 配置文件。
  echo        请将 server\.env.customer 的内容复制为 server\.env。
  echo.
  pause
  exit /b 1
)

REM ---------- 首次安装桥接层依赖 ----------
if not exist "%BRIDGE_DIR%node_modules" (
  echo [1/3] 首次运行，正在安装 桥接层 依赖...
  pushd "%BRIDGE_DIR%"
  call npm install
  popd
  echo.
)

REM ---------- 检查前端构建产物 dist ----------
if not exist "%~dp0dist\index.html" (
  echo [2/3] 未找到前端构建产物 dist，正在构建（需联网）...
  call npm install
  call npm run build
  echo.
) else (
  echo [2/3] 前端构建产物已存在。
)

REM ---------- 启动桥接层（同时托管前端 + 转发本地后端 + 连远程MySQL） ----------
echo [3/3] 启动桥接层...
echo.
echo 请先在本机启动桌面后端 Fantnel.exe（端口 13521）。
echo.
echo 打开浏览器访问: http://localhost:3939
echo 登录/注册:      http://localhost:3939/site-login
echo 按 Ctrl+C 可停止。
echo.
pushd "%BRIDGE_DIR%"
call npm start
popd