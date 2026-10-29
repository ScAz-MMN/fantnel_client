# FantnelLauncher 交接文档

本文档记录 `FantnelLauncher`（.NET 8 WPF 桌面启动器）的架构、各方法职责、已知问题与排查方法，供后续开发/排障直接查阅，避免重复踩坑。

## 项目定位

把「检查 Node.js → 静默运行 `st.bat` → 等前端就绪 → WebView2 内嵌加载 `localhost:5173`」封装成一个仿 macOS 无边框窗口的桌面程序。程序是**自包含**的：`csproj` 的 `CopyFantnelResources` 目标会在构建后把根目录的 `st.bat`、`back\`、`web\` 复制到输出目录，因此整个输出目录可直接搬走使用。

## 源码文件

```
FantnelLauncher/
├── MainWindow.xaml       主窗口布局（无边框标题栏 + 红/黄/绿圆点按钮 + WebView2）
├── MainWindow.xaml.cs    全部启动/加载/窗口逻辑（核心，约 490 行）
├── App.xaml(.cs)         应用入口
├── app.manifest          以普通用户权限运行（asInvoker），不申请管理员
├── FantnelLauncher.csproj
└── build.bat              dotnet build
```

## 构建

```bash
cd "C:\Users\XiaoXin\Desktop\Fantnel"
dotnet build "FantnelLauncher\FantnelLauncher.csproj" -c Release
```

输出：`FantnelLauncher\bin\Release\net8.0-windows\FantnelLauncher.exe`

> **重要**：`dotnet build` 会清空输出目录，所以**每次构建后都要重新确认**输出目录里的 `st.bat`、`web\vite.config.js` 与根目录一致（`CopyFantnelResources` 会自动重拷，但 vite.config.js 的改动历史上有过被回退的教训）。

## 启动流程（`MainWindow_Loaded`）

按顺序执行，每步写日志到 `launcher-error.log`：

1. **权限检查**：`IsElevated` 检测当前是否以管理员运行，若是则 `RelaunchAsNormalUser()`（通过 `explorer.exe` 以普通用户重新启动）并关闭自身。
2. `KillStaleLauncherProcesses()`：杀其他 `FantnelLauncher` 实例（排除当前 PID）。
3. `NodeJsInstalled()`：检查 Node.js 是否安装（查 PATH 里的 node.exe）。
4. `KillProcessesOnPorts()`：基于 `IPGlobalProperties` 杀掉占用 `5173/3939/13521` 的进程（残留的 node/Fantnel）。
5. `RunStBatSilently()`：以 `cmd /c st.bat`、`CreateNoWindow` 静默运行同目录 `st.bat`，启动 3 个组件。
   - 5b. `ElevatedBrowserRunning()`：若检测到已有「管理员权限」的浏览器/WebView2 进程在跑，弹窗提示用户统一权限后重试。
6. `FindReachableUrlAsync(NodeUrlCandidates, 45s)`：轮询 `127.0.0.1:5173` 和 `localhost:5173`，返回第一个可达的 URL。
7. `KillWebView2Processes()`：只杀 `msedgewebview2`（不动用户的 `msedge.exe`），并等待每个进程真正退出，释放共享的 WebView2 运行时。
8. `await InitializeWebViewAsync(url)`：初始化 WebView2 并加载页面。

全程 `try/catch`：出错时写日志 + `MessageBox` 提示，并记录日志文件路径。

## 核心方法职责

| 方法 | 职责 |
|------|------|
| `Log(string)` | 分 3 处写日志：`AppDir\launcher-error.log`、`%LocalAppData%\FantnelLauncher\launcher-error.log`、`%TEMP%\FantnelLauncher\launcher-error.log`，返回成功路径 |
| `Describe(Exception)` | 返回 `ex.ToString()` 完整堆栈 |
| `MainWindow_Loaded` | 启动总流程（见上） |
| `NodeJsInstalled()` | 检查 node.exe 是否存在 |
| `KillProcessesOnPorts()` | 按端口杀残留 node/Fantnel 进程 |
| `KillStaleLauncherProcesses()` | 杀重复的启动器实例 |
| `KillWebView2Processes()` | 杀 `msedgewebview2` 并等进程退出 |
| `RunStBatSilently()` | `cmd /c st.bat` + `CreateNoWindow` 静默运行 |
| `FindReachableUrlAsync(candidates, timeout)` | 轮询候选 URL，返回可达者（或 null） |
| `InitializeWebViewAsync(url)` | 创建 WebView2 环境 + 控制器；`EnsureCoreWebView2Async` 包 **5 次重试**（`COMException` 时杀进程、等 1.5s 再试）；成功后 `NavigationCompleted` 成功才 `Visibility=Visible`；失败给用户「改用系统默认浏览器打开」回退选项 |
| `GetWebView2RuntimeVersion()` | 读取 WebView2 运行时版本，用于诊断日志 |
| `IsElevated` / `RelaunchAsNormalUser()` | 管理员权限检测与降级重启 |
| `ElevatedBrowserRunning()` / `IsProcessElevated(p)` | 通过 `OpenProcessToken`/`TokenElevation`/`TokenQuery` 检测是否有提升权限的浏览器进程 |
| `TitleBar_MouseLeftButtonDown/Up/Move` | 无边框窗口手写拖拽（`DragMove`，位移 >1px 才触发） |
| `MacButton_Click` | 按 `CommandParameter` 分发 close / minimize / maximize |
| `ToggleMaximize()` | 手动最大化：仅铺满 `SystemParameters.WorkArea`（**不遮挡任务栏**），窗口仍保持 `Normal` 状态，保存/恢复 `_normalBounds` |

## 窗口实现

- `WindowStyle="None"` 无边框 + 自定义 44px 标题栏 + 右上角红/黄/绿圆点（关闭/最小化/最大化）。
- 最大化不遮挡任务栏：用 `ToggleMaximize()` 手动把窗口移动到工作区并拉伸，而非 `WindowState=Maximized`。
- `ResizeMode="CanResizeWithGrip"`：右下角缩放柄。
- **未使用 `WindowChrome`**：它与 WebView2 创建控制器（挂 HWND 到窗口消息循环）存在已文档化的不兼容，会触发 `0x800700AA`。改用上面手写拖拽方案规避。

## 关键字段

```csharp
NodeUrlCandidates = { "http://127.0.0.1:5173", "http://localhost:5173" };
FantnelPorts       = { 5173, 3939, 13521 };
AppDir             = AppDomain.CurrentDomain.BaseDirectory;
```

## 日志

程序启动流程分步写日志。查看日志文件：

- `FantnelLauncher\bin\Release\net8.0-windows\launcher-error.log`
- 或 `%LocalAppData%\FantnelLauncher\launcher-error.log`
- 或 `%TEMP%\FantnelLauncher\launcher-error.log`

## 已知问题（重要）

**`0x800700AA` 崩溃 — WebView2 起不来（当前未解决，准备换环境调试）**

- 现象：`CreateCoreWebView2ControllerAsync(ParentWindow)` 抛 `System.Runtime.InteropServices.COMException (0x800700AA)`「请求的资源在使用中」，webview 从未加载。
- 日志：`"The WebView2 process crashed and the application needs to be restarted."`，且 Windows 弹出**「系统在此应用程序中检测到基于堆栈的缓冲区溢出」**。
- 结论：**这是机器级问题，非应用代码 bug**。「基于堆栈的缓冲区溢出」是 Windows 对原生进程的 DEP/GS 防护，.NET 托管代码不可能触发——崩溃发生在 `msedgewebview2.exe`（WebView2 浏览器子进程）启动阶段。它与管理员/普通权限、窗口样式、进程清理、重试次数都无关。
- 判定为 WebView2 运行时本身与安全软件（360 / 火绒 / 电脑管家等的浏览器保护/注入）冲突。
- 应用侧已做的防御（都保留了）：5 次重试、杀残留 `msedgewebview2`、提升权限浏览器检测弹窗、失败诊断日志（打印 `elevated browsers detected at failure` 和 `WebView2 runtime version`）、失败回退「改用系统默认浏览器打开」。
- 机器侧修复方向（需用户在出问题的那台机器上做）：
  1. 重装 WebView2 Evergreen 运行时；
  2. 排查并关闭安全软件的浏览器注入 / 浏览器保护；
  3. 修复或重装 Edge；
  4. 重启系统。

## 需要用户知晓的固定约束

- **`st.bat` 不可改动**：它已被用户调回「可见版」（`cmd /k` + `pause`，开三个窗口）。程序运行它时用 `CreateNoWindow` 静默，但脚本本身保持原样。
- **不加管理员权限**：`app.manifest` 使用 `asInvoker`。之前用户反映过「管理员 Edge 与普通权限 Edge 冲突」的弹窗，因此启动器特意用普通权限运行并做降级重启。

## 关联文档

- 根目录 `README.md`：项目总览（目录、端口、一键启动、构建、依赖）。
- `web/server/README.md`：MySQL 桥接层说明（注意：该文件是 GBK 编码，用 UTF-8 打开会乱码）。