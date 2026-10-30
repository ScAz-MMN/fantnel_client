# Fantnel（Linux 版）

桌面应用（Fantnel）的 **Linux 移植版**，带一个 `.NET 10`（Avalonia）启动器（`FantnelLauncher`）。一键打开三个终端窗口分别拉起后端、前端、MySQL 桥接，并在内嵌浏览器（WebKitGTK）中加载前端。

> 本目录是独立的 Linux 版本，与 Windows 版（项目根目录）互不影响。Windows 版文档见项目根目录的 `README.md`。

## 目录结构

```
FantnelLauncher For Linux/
├── st.sh                一键启动脚本——为 3 个组件各开一个终端窗口
├── back/                已编译的 .NET 后端（Linux x64）
│   ├── fantnel/         后端程序集（Fantnel.dll + 原生二进制 + 依赖 dll）
│   └── fantnel.sh       启动后端（cd 到 fantnel 后 dotnet Fantnel.dll）
├── web/                 前端（Vue3 + Vite）与 MySQL 桥接层
│   ├── B.sh             npm run dev（启动前端 dev server）
│   ├── vite.config.js   Vite 配置（端口 5173、代理规则）
│   ├── server/          MySQL 登录/注册 + 收藏同步桥接层（端口 3939）
│   │   ├── A.sh         npm start（启动桥接层）
│   │   └── README.md    桥接层说明
│   └── src/             前端源码
├── FantnelLauncher/     .NET 10 Avalonia 启动器（本项目核心源码）
│   ├── MainWindow.axaml(.cs)   主窗口 + 启动/加载逻辑
│   ├── App.axaml(.cs)
│   ├── Program.cs
│   └── FantnelLauncher.csproj
└── README.md            本文档
```

## 组件与端口

| 组件 | 位置 | 启动命令 | 端口 |
|------|------|----------|------|
| 后端 | `back/fantnel` | st.sh 第 1 项（`dotnet Fantnel.dll`） | 13521 |
| 前端 | `web/B.sh` | st.sh 第 2 项（`npm run dev`） | 5173 |
| MySQL 桥接层 | `web/server/A.sh` | st.sh 第 3 项（`npm start`） | 3939 |

前端 `vite.config.js` 的代理规则：
- `/api/auth`、`/api/favorites` → `http://localhost:3939`（MySQL 桥接层）
- 其余 `/api`、`/image` → `http://localhost:13521`（原桌面后端）

> MySQL 桥接层首次使用需先用 root 执行一次 `web/server/setup.sql` 给远程用户授权，连接信息见 `web/server/.env` 与 `web/server/README.md`。

## 一键启动（推荐）

在终端执行（或双击）：

```bash
cd "FantnelLauncher For Linux"
bash st.sh        # 或 chmod +x st.sh && ./st.sh
```

`st.sh` 会**为每个组件各打开一个独立的终端窗口**：

1. `Fantnel-Backend` —— `cd back/fantnel && dotnet Fantnel.dll`
2. `Fantnel-Web` —— `cd web && bash B.sh`（即 `npm run dev`）
3. `Fantnel-MySQL-Bridge` —— `cd web/server && bash A.sh`（即 `npm start`）

前端就绪后浏览器访问 `http://localhost:5173`。想停掉某个组件，直接关闭它对应的终端窗口即可。

> 若系统没有桌面终端（gnome-terminal / xfce4-terminal / mate-terminal / konsole / xterm / kitty），`st.sh` 会自动退化为在后台运行三个组件。

## 启动器（FantnelLauncher）

`FantnelLauncher` 是一个 **.NET 10 Avalonia** 桌面启动器，把上面三步封装成桌面程序：

1. 检查本机是否安装 Node.js。
2. 静默运行同目录下的 `st.sh`（`/bin/bash st.sh`、`CreateNoWindow`）。
3. 等待前端就绪（轮询 `http://127.0.0.1:5173` / `http://localhost:5173`，最长 45 秒）。
4. 用内嵌 WebView（Avalonia `NativeWebView`，Linux 上使用 **WebKitGTK**）加载前端页面。

自定义无边框窗口（仿 macOS）：标题栏拖拽、右上角红/黄/绿圆点（关闭 / 最小化 / 最大化），最大化仅铺满工作区（不遮挡任务栏/面板）。若内嵌 WebView 无法初始化，会回退用系统默认浏览器打开前端地址。

### 构建启动器

```bash
cd "C:\Users\XiaoXin\Desktop\Fantnel\FantnelLauncher For Linux"
dotnet build "FantnelLauncher\FantnelLauncher.csproj" -c Release
```

输出：

```
FantnelLauncher\bin\Release\net10.0\FantnelLauncher.dll
FantnelLauncher\bin\Release\net10.0\FantnelLauncher.exe   # 平台可执行壳
```

构建时 `csproj` 的 `CopyFantnelResources` 目标会自动把 `st.sh`、`back\`、`web\` 复制到输出目录，因此生成目录是自包含的。构建需 .NET 10 SDK。

## 依赖（Linux）

- [.NET 10 SDK/Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)（构建与运行都需要）
- [Node.js](https://nodejs.org/)（前端 dev server 与桥接层需要）
- WebKitGTK（Avalonia `NativeWebView` 的运行依赖）
- MySQL（供桥接层连接）
- 桌面环境（GNOME / XFCE 等）与一个终端模拟器（用于 `st.sh` 打开多窗口）

## 更多

- 后端 Linux 构建来自 `Fantnel.linux.x64.zip`（解压进 `back/`）。
- 启动器实现细节见 `FantnelLauncher/HANDOVER.md`。