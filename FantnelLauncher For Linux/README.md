# Fantnel

桌面应用（Fantnel），带一个 `.NET 8` 启动器（`FantnelLauncher`），一键拉起三个组件并在内嵌浏览器（WebView2）中加载前端。

## 目录结构

```
Fantnel/
├── st.bat              一键启动脚本（启动 3 个组件）——不要改动
├── back/               已编译的 .NET 后端（Fantnel.exe + 依赖 dll），非本项目源码
├── web/                前端（Vue3 + Vite）与 MySQL 桥接层
│   ├── B.bat           npm run dev（启动前端 dev server）
│   ├── vite.config.js  Vite 配置（端口 5173、代理规则）
│   ├── server/         MySQL 登录/注册 + 收藏同步桥接层（端口 3939）
│   │   ├── A.bat       npm start（启动桥接层）
│   │   └── README.md   桥接层说明
│   └── src/            前端源码
├── FantnelLauncher/    .NET 8 WPF 启动器（本项目核心源码）
│   ├── MainWindow.xaml(.cs)   主窗口 + 启动/加载逻辑
│   ├── App.xaml(.cs)
│   ├── app.manifest           以普通用户权限运行（asInvoker）
│   ├── FantnelLauncher.csproj
│   └── build.bat               dotnet build
└── README.md           本文档
```

## 组件与端口

| 组件 | 位置 | 启动命令 | 端口 |
|------|------|----------|------|
| 后端 | `back\Fantnel.exe` | st.bat 第 1 项 | 13521 |
| 前端 | `web\B.bat` | st.bat 第 2 项 | 5173 |
| MySQL 桥接层 | `web\server\A.bat` | st.bat 第 3 项 | 3939 |

前端 `vite.config.js` 的代理规则：
- `/api/auth`、`/api/favorites` → `http://localhost:3939`（MySQL 桥接层）
- 其余 `/api`、`/image` → `http://localhost:13521`（原桌面后端）

> MySQL 桥接层首次使用需先用 root 执行一次 `web/server/setup.sql` 给 `fantnelp` 授权，连接信息见 `web/server/.env` 与 `web/server/README.md`。

## 一键启动（推荐）

双击项目根目录的 `st.bat`，它会依次打开三个窗口：

1. `Fantnel-Backend`（`cmd /k back\Fantnel.exe`）
2. `Fantnel-Web`（`cmd /k web\B.bat`，即 `npm run dev`）
3. `Fantnel-MySQL-Bridge`（`cmd /k web\server\A.bat`，即 `npm start`）

然后浏览器访问 `http://localhost:5173`。

## 启动器（FantnelLauncher）

`FantnelLauncher` 是一个 .NET 8 WPF 自包含启动器，把上面三步封装成桌面程序：

1. 检查本机是否安装 Node.js。
2. 静默运行同目录下的 `st.bat`（`cmd /c`、`CreateNoWindow`）。
3. 等待前端就绪（轮询 `http://localhost:5173`，最长 45 秒）。
4. 用 WebView2 内嵌浏览器加载前端页面。

自定义无边框窗口（仿 macOS）：手写拖拽、右下角缩放柄、右上角红/黄/绿圆点（关闭 / 最小化 / 最大化），最大化仅铺满工作区（不遮挡任务栏）。

### 构建启动器

```bash
cd "C:\Users\XiaoXin\Desktop\Fantnel"
dotnet build "FantnelLauncher\FantnelLauncher.csproj" -c Release
```

或直接双击 `FantnelLauncher\build.bat`。

输出：

```
FantnelLauncher\bin\Release\net8.0-windows\FantnelLauncher.exe
```

构建时 `csproj` 的 `CopyFantnelResources` 目标会自动把 `st.bat`、`back\`、`web\` 复制到输出目录，因此生成目录是自包含的：把整个输出目录拷到任何装有 .NET 8 桌面运行时 + Node.js 的机器上，双击 `FantnelLauncher.exe` 即可。

> 注意：构建会清空输出目录，所以必须在构建后重新确认输出目录里的 `st.bat`、`web\vite.config.js` 与根目录一致。

## 依赖

- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)（构建需 .NET 8 SDK）
- [Node.js](https://nodejs.org/)（前端 dev server 与桥接层需要）
- WebView2 Evergreen Runtime（Windows 10/11 通常已内置）
- MySQL（供桥接层连接）

## 已知问题

- 在某些机器上，`msedgewebview2.exe` 启动时会触发 Windows 的"基于堆栈的缓冲区溢出"检测，导致 WebView2 内嵌浏览器无法创建（`0x800700AA`）。这是机器级 WebView2 运行时 / 安全软件（360 / 火绒 / 电脑管家等浏览器保护）冲突，与应用代码无关。已加入 5 次重试与诊断日志。解决办法：重装 WebView2 Evergreen 运行时、排查并关闭安全软件的浏览器注入、修复 / 重装 Edge、重启。

## 更多

- 启动器实现细节与各方法说明见 `FantnelLauncher\HANDOVER.md`。