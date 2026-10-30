# FantnelLauncher（Linux 版）交接文档

Avalonia (.NET 10) 桌面启动器，用于一键拉起 Fantnel 的三个组件并在内嵌 WebView（WebKitGTK）中加载前端。

## 技术栈

- **框架**：.NET 10（`net10.0`），自包含发布（`RuntimeIdentifiers=linux-x64`）
- **UI**：Avalonia 12.0.0 + `Avalonia.Controls.WebView` 12.1.1
- **WebView 后端**：Linux 上 `NativeWebView` 使用 **WebKitGTK**
- **包**：Avalonia、Avalonia.Desktop、Avalonia.Themes.Fluent、Avalonia.Controls.WebView

## 源码文件

| 文件 | 作用 |
|------|------|
| `Program.cs` | 入口，`AppBuilder.Configure<App>().UsePlatformDetect()` 启动 |
| `App.axaml(.cs)` | 应用/主题（Fluent Light），创建 `MainWindow` |
| `MainWindow.axaml` | 无边框窗口 + 仿 MAC 标题栏（红/黄/绿圆点）+ `NativeWebView` |
| `MainWindow.axaml.cs` | 启动管线 + 标题栏交互 + 日志 |
| `FantnelLauncher.csproj` | 目标框架、包引用、`CopyFantnelResources` 资源复制目标 |
| `build.bat` | `dotnet build`（快捷脚本） |

## 启动管线（MainWindow.axaml.cs）

`Opened` 事件触发，顺序执行：

1. **Node 检查** `NodeJsInstalled()`：运行 `node --version`，输出以 `v` 开头即通过。
2. **清理残留进程** `KillProcessesOnPorts()`：用 Linux 的 `ss -ltnp` 找出监听端口 `{5173, 3939, 13521}` 的进程 PID 并结束（清除上次未退干净的后端/前端）。
3. **启动组件** `RunStSh()`：`/bin/bash st.sh`（`UseShellExecute=false`、`CreateNoWindow=true`），`st.sh` 为三个组件各开一个终端窗口。
4. **等待就绪** `FindReachableUrlAsync()`：轮询 `http://127.0.0.1:5173` → `http://localhost:5173`，最长 45 秒。
5. **加载 WebView**：`Web.Source = new Uri(url)`；`EnvironmentRequested` 里 `EnableDevTools = true`；`NavigationCompleted` 成功后 `Web.IsVisible = true`。若 WebView 抛异常，回退 `xdg-open` 用系统浏览器打开前端地址。

## 仿 MAC 标题栏

- `SystemDecorations="None"`（Avalonia 12 用 `WindowDecorations="None"`）实现无边框。
- 标题栏 `Border` 的 `PointerPressed` → `BeginMoveDrag(e)` 实现拖拽。
- 右上角三个圆点按钮（`Button.MacDot` 样式：13×13、圆角 6.5、红 `#FF5F57` / 黄 `#FEBC2E` / 绿 `#28C840`）：
  - `close` → `Close()`
  - `minimize` → `WindowState.Minimized`
  - `maximize` → `ToggleMaximize()`：最大化到主屏幕 `WorkingArea`（`Position/Width/Height`），不遮挡任务栏/面板；记录恢复前的正常位置与尺寸。

## 与 Windows 版的差异

- 去掉了 WebView2 / Edge 相关逻辑（`IsElevated`、`RelaunchAsNormalUser`、`ElevatedBrowserRunning`、`KillWebView2Processes`）——Linux 的 WebKitGTK 不存在提升 Edge 冲突。
- 端口清理改用 `ss -ltnp`（Linux）替代 WPF 版的 `IPGlobalProperties` 逐连接匹配。
- 后端进程名在 Linux 上是 `dotnet`，启动脚本改为 `st.sh`（多终端窗口版）。

## 构建

```bash
cd "FantnelLauncher For Linux"
dotnet build "FantnelLauncher\FantnelLauncher.csproj" -c Release
```

输出在 `FantnelLauncher\bin\Release\net10.0\`，含 `FantnelLauncher.dll` + 平台可执行壳 + `st.sh` + `back/` + `web/`（自包含）。

## 已知事项

- `st.sh` 依赖桌面终端模拟器（gnome-terminal / xfce4-terminal / mate-terminal / konsole / xterm / kitty），没有时自动退化为后台运行。
- 后端需 .NET 10 运行时（`back/fantnel/Fantnel.runtimeconfig.json` 目标框架为 `net10.0`）。
- WebView（WebKitGTK）在目标机上需已安装对应系统依赖；缺失时启动器会回退到系统浏览器。