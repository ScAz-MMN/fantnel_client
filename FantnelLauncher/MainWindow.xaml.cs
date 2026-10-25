using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FantnelLauncher;

public partial class MainWindow : Window
{
    private static readonly string[] NodeUrlCandidates = { "http://127.0.0.1:5173", "http://localhost:5173" };
    private static readonly int[] FantnelPorts = { 5173, 3939, 13521 };
    private static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Appends an exception's details to a log file. Writes to several reliable
    /// locations (next to the exe, %LocalAppData%, %TEMP%) so at least one succeeds,
    /// and returns the first path that was written (for the error dialog).
    /// </summary>
    private static string Log(string message)
    {
        var stamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        string? written = null;

        string[] candidates =
        {
            Path.Combine(AppDir, "launcher-error.log"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FantnelLauncher", "launcher-error.log"),
            Path.Combine(Path.GetTempPath(), "FantnelLauncher", "launcher-error.log"),
        };

        foreach (var path in candidates)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, stamp);
                written ??= path;
            }
            catch
            {
                // Try the next location; never let logging break startup.
            }
        }
        return written ?? "（无法写入日志）";
    }

    /// <summary>Formats an exception (with inner details and stack) for the log.</summary>
    private static string Describe(Exception ex)
        => ex.ToString();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Log("launcher started");

            // 0. If this app was started with admin rights (Run-as-administrator),
            //    relaunch it as a normal user. Running elevated makes WebView2/Edge
            //    start elevated processes, which then clash with normal-mode Edge and
            //    triggers the "two permission modes cannot coexist" dialog.
            if (IsElevated)
            {
                RelaunchAsNormalUser();
                Application.Current.Shutdown();
                return;
            }

            // 0b. If an earlier run of this launcher is still alive (hung/frozen), it keeps
            //     holding the WebView2 runtime, so a new run fails with
            //     "0x800700AA 请求的资源在使用中". Kill any stale instance except this one.
            KillStaleLauncherProcesses();

            // 1. Check Node.js
            if (!NodeJsInstalled())
            {
                MessageBox.Show(
                    "未检测到 Node.js。请安装 Node.js (https://nodejs.org) 后重新启动本程序。",
                    "缺少 Node.js",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 2. Clear any leftover processes holding the ports (from previous runs).
            KillProcessesOnPorts();

            // 3. Run st.bat silently (in the same directory as this exe).
            RunStBatSilently();

            // 4. Poll the frontend until one of its URLs is reachable.
            //    Vite may bind to either IPv4 (127.0.0.1) or IPv6 (::1 / localhost);
            //    WebView2 resolves "localhost" differently, so probe both and load
            //    whichever actually answers.
            string? url = await FindReachableUrlAsync(NodeUrlCandidates, TimeSpan.FromSeconds(45));
            if (url is null)
            {
                MessageBox.Show(
                    $"无法连接到前端服务 (5173)。\n\n" +
                    "可能原因：st.bat 中的前端/后端/桥接服务启动失败。\n" +
                    "请手工运行 web\\B.bat 查看报错。",
                    "服务未就绪",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 5. Clear any lingering WebView2 processes. If an earlier run of this
            //    app (or Edge) was elevated, a leftover elevated msedgewebview2 process
            //    makes normal-mode WebView2 refuse to start and triggers the
            //    "two permission modes cannot coexist" dialog.
            Log("frontend ready, clearing stale WebView2 processes");
            KillWebView2Processes();

            // 5b. If a browser process is STILL running elevated (we cannot kill elevated
            //     processes from a normal-privilege app), a normal WebView2 controller
            //     cannot be created and fails with 0x800700AA. Warn the user with the fix.
            if (ElevatedBrowserRunning())
            {
                Log("elevated WebView2/Edge process detected - cannot create controller");
                MessageBox.Show(
                    "检测到 Edge/WebView2 正在以管理员(提升)权限运行。\n\n" +
                    "普通权限的程序无法在管理员权限的浏览器进程运行时创建 WebView2，\n" +
                    "导致启动失败 (0x800700AA)。\n\n" +
                    "请按下面任一方法解决：\n" +
                    "1) 关闭所有 Edge 窗口后重试；\n" +
                    "2) 若系统弹出\"统一切换为普通权限重启\"提示，点击它；\n" +
                    "3) 重启电脑（会清掉残留的管理员权限浏览器进程）。",
                    "Edge 权限冲突",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // 6. Load the frontend in WebView2.
            await InitializeWebViewAsync(url);
            Log("webview initialized OK");
        }
        catch (Exception ex)
        {
            string logPath = Log(Describe(ex));
            MessageBox.Show(
                "启动失败:\n" + ex.Message + "\n\n详细日志: " + logPath,
                "错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ---------- MAC-style title bar ----------

    // WindowChrome is intentionally NOT used (it clashes with WebView2 and makes
    // controller creation fail with 0x800700AA), so window dragging and the
    // maximize button are implemented by hand:
    //   - drag: TitleBar mouse handlers + DragMove()
    //   - resize: the native bottom-right grip (ResizeMode=CanResizeWithGrip)
    //   - maximize: move the window onto the work area so it never covers the taskbar

    private bool _dragInProgress;
    private Point _dragStartPoint;

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Let a double-click on the title bar maximize/restore, like a normal title bar.
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }
        _dragInProgress = true;
        _dragStartPoint = e.GetPosition(this);
        // A single left-click should not start a drag, so we do NOT DragMove() here.
    }

    private void TitleBar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragInProgress = false;
    }

    private void TitleBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragInProgress) return;

        var pos = e.GetPosition(this);
        double dx = pos.X - _dragStartPoint.X;
        double dy = pos.Y - _dragStartPoint.Y;
        if (Math.Abs(dx) < 1 && Math.Abs(dy) < 1) return;

        // The pointer has moved enough to be a real drag: hand off to DragMove().
        _dragInProgress = false; // will be re-armed only by a new MouseDown
        try
        {
            DragMove();
        }
        catch
        {
            // DragMove throws if the button was released mid-flight; ignore.
        }
    }

    // Bounds of the window while it is in the normal (un-maximized) state, so we can
    // restore them after un-maximizing, plus whether we are currently "maximized".
    private Rect _normalBounds;
    private bool _isMaximized;

    private void MacButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;

        switch (btn.CommandParameter as string)
        {
            case "close":
                Close();
                break;
            case "minimize":
                WindowState = WindowState.Minimized;
                break;
            case "maximize":
                ToggleMaximize();
                break;
        }
    }

    private void ToggleMaximize()
    {
        var work = SystemParameters.WorkArea; // area excluding the taskbar

        if (_isMaximized)
        {
            // Restore to the pre-maximized bounds.
            Left = _normalBounds.Left;
            Top = _normalBounds.Top;
            Width = _normalBounds.Width;
            Height = _normalBounds.Height;
            _isMaximized = false;
        }
        else
        {
            _normalBounds = new Rect(Left, Top, Width, Height);
            // Maximize by hand onto the work area so the taskbar stays visible.
            // We deliberately keep WindowState=Normal and just move/resize the window,
            // because a WindowStyle=None window set to Maximized would cover the taskbar.
            Left = work.Left;
            Top = work.Top;
            Width = work.Width;
            Height = work.Height;
            _isMaximized = true;
        }
    }

    // ---------- Startup helpers ----------

    private static bool NodeJsInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "--version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return false;
            proc.WaitForExit(5000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Kills any process that is currently listening on the ports Fantnel uses.
    /// Clears stale dev servers / backends left over from earlier runs that could
    /// otherwise keep 5173 bound to a stale address.
    /// </summary>
    private static void KillProcessesOnPorts()
    {
        try
        {
            var used = IPGlobalProperties
                .GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Where(e => FantnelPorts.Contains(e.Port))
                .Select(e => e.Port)
                .Distinct()
                .ToList();

            if (used.Count == 0) return;

            var rows = Process.GetProcessesByName("node")
                .Concat(Process.GetProcessesByName("Fantnel"))
                .Concat(Process.GetProcessesByName("Fantnel.exe"))
                .Distinct();
            foreach (var p in rows)
            {
                try { p.Kill(); } catch { /* ignore */ }
            }
        }
        catch
        {
            // Best-effort cleanup only; never block startup on it.
        }
    }

    /// <summary>
    /// Kills any OTHER instance of this launcher that is still alive. A previous run
    /// that hung/froze keeps holding the WebView2 runtime, which makes a new run fail
    /// with "0x800700AA 请求的资源在使用中". The current process is excluded.
    /// </summary>
    private static void KillStaleLauncherProcesses()
    {
        try
        {
            int currentId = Environment.ProcessId;
            foreach (var p in Process.GetProcessesByName("FantnelLauncher"))
            {
                try
                {
                    if (p.Id != currentId)
                    {
                        p.Kill();
                        p.WaitForExit(2000);
                    }
                }
                catch { /* ignore */ }
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    /// <summary>
    /// Kills any lingering WebView2 browser processes so a fresh, clean browser
    /// instance is created, then WAITS for them to actually exit. Only WebView2's
    /// own process pool (msedgewebview2.exe) is closed; the user's Edge browser
    /// (msedge.exe) is deliberately left untouched.
    /// </summary>
    private static void KillWebView2Processes()
    {
        try
        {
            var list = Process.GetProcessesByName("msedgewebview2").ToList();
            foreach (var p in list)
            {
                try { p.Kill(); } catch { /* may be elevated -> access denied; ignore */ }
            }
            // Give the killed processes a moment to fully terminate and release the
            // shared WebView2 runtime before a new environment is created.
            foreach (var p in list)
            {
                try { p.WaitForExit(3000); } catch { }
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    /// <summary>
    /// True if any WebView2/Edge browser process is running elevated (admin). A normal
    /// (non-elevated) launcher CANNOT create a WebView2 controller while an elevated
    /// browser process holds the shared runtime — it fails with 0x800700AA. We detect it
    /// here so we can give the user a clear, actionable message instead of a cryptic COM error.
    /// </summary>
    private static bool ElevatedBrowserRunning()
    {
        try
        {
            foreach (var name in new[] { "msedgewebview2", "msedge" })
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (IsProcessElevated(p)) return true;
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch
        {
            // On failure, assume no elevated browser so we do not block startup.
        }
        return false;
    }

    /// <summary>True if the given process is running with an elevated (admin) token.</summary>
    private static bool IsProcessElevated(Process process)
    {
        if (!OpenProcessToken(process.Handle, TokenQuery, out IntPtr token)) return false;
        try
        {
            int size = Marshal.SizeOf<int>();
            IntPtr buf = Marshal.AllocHGlobal(size);
            try
            {
                if (!GetTokenInformation(token, TokenElevation, buf, (uint)size, out _))
                    return false;
                return Marshal.ReadInt32(buf) != 0;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }
        finally
        {
            CloseHandle(token);
        }
    }

    /// <summary>
    /// Runs st.bat from the exe's directory in a hidden console window and returns immediately.
    /// </summary>
    private static void RunStBatSilently()
    {
        string batPath = Path.Combine(AppDir, "st.bat");

        if (!File.Exists(batPath))
        {
            // Fall back to the working directory in case the app was run without publishing.
            batPath = Path.Combine(Environment.CurrentDirectory, "st.bat");
        }

        if (!File.Exists(batPath))
        {
            throw new FileNotFoundException("未找到 st.bat 文件，请确保它与程序位于同一目录。", batPath);
        }

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"\"{batPath}\"\"",
            WorkingDirectory = Path.GetDirectoryName(batPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        // Let the batch's spawned processes (npm/dev server) keep running independently.
        Process.Start(psi);
    }

    /// <summary>
    /// Polls each candidate URL until one answers, then returns that working URL
    /// (or null on timeout). Because vite may bind to IPv4 or IPv6 loopback, we try
    /// both "127.0.0.1" and "localhost" so WebView2 gets a URL it can definitely reach.
    /// </summary>
    private static async Task<string?> FindReachableUrlAsync(string[] candidates, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        while (DateTime.UtcNow < deadline)
        {
            foreach (var url in candidates)
            {
                try
                {
                    using var resp = await client.GetAsync(url);
                    if (resp.IsSuccessStatusCode) return url;
                }
                catch
                {
                    // Not up yet — keep polling.
                }
            }
            await Task.Delay(500);
        }
        return null;
    }

    private async Task InitializeWebViewAsync(string url)
    {
        try
        {
            // Give WebView2 its OWN user-data folder, fresh for each launch. This isolates the
            // browser processes from the system Edge (avoiding the elevated-Edge conflict)
            // and uses a brand-new folder every run so a folder left locked by an earlier
            // crashed/hung WebView2 instance can never block startup again.
            string baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FantnelLauncher",
                "WebView2");
            string userDataFolder = Path.Combine(baseFolder, DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            // Tidy up abandoned per-launch folders older than a week (best-effort).
            try
            {
                if (Directory.Exists(baseFolder))
                {
                    foreach (var dir in Directory.GetDirectories(baseFolder))
                    {
                        try
                        {
                            if (DateTime.UtcNow - Directory.GetLastWriteTimeUtc(dir) > TimeSpan.FromDays(7))
                                Directory.Delete(dir, true);
                        }
                        catch { /* folder may be in use; ignore */ }
                    }
                }
            }
            catch { /* best-effort */ }

            Log("CreateAsync: " + userDataFolder);
            var env = await Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .CreateAsync(null, userDataFolder, null);
            Log("CreateAsync OK");

            // Creating the controller can transiently fail with 0x800700AA (resource busy)
            // right after the stale WebView2 processes are killed, because the shared
            // runtime is still tearing down. Retry a few times with a short pause.
            const int maxAttempts = 5;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await webView.EnsureCoreWebView2Async(env);
                    break;
                }
                catch (System.Runtime.InteropServices.COMException)
                    when (attempt < maxAttempts)
                {
                    Log($"EnsureCoreWebView2Async attempt {attempt} failed (busy), retrying...");
                    KillWebView2Processes();
                    await Task.Delay(1500);
                }
            }
            Log("EnsureCoreWebView2Async OK");
        }
        catch (Microsoft.Web.WebView2.Core.WebView2RuntimeNotFoundException wvEx)
        {
            Log(Describe(wvEx));
            MessageBox.Show(
                "缺少 Microsoft Edge WebView2 运行时:\n" + wvEx.Message +
                "\n\n请前往 https://developer.microsoft.com/microsoft-edge/webview2 下载并安装。",
                "缺少 WebView2",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }
        catch (Exception ex)
        {
            string logPath = Log(Describe(ex));
            Log("elevated browsers detected at failure: " + ElevatedBrowserRunning());
            Log("WebView2 runtime version: " + GetWebView2RuntimeVersion());

            // WebView2 repeatedly failed to create a controller (typically 0x800700AA).
            // This is almost always a broken/conflicted WebView2 runtime on the machine,
            // not something the app can fix from code. Give the user a clear choice:
            // open the page in the system browser as a fallback, or inspect the log.
            var result = MessageBox.Show(
                "WebView2 初始化失败（0x800700AA 请求的资源在使用中）。\n\n" +
                "详细日志: " + logPath + "\n\n" +
                "这通常是本机 WebView2/Edge 运行时状态异常导致，与应用代码无关。\n" +
                "修复建议：\n" +
                "  1) 关闭所有 Edge 窗口后重试；\n" +
                "  2) 若弹出\"统一切换为普通权限重启\"提示请点击它；\n" +
                "  3) 重装 WebView2 运行时 https://developer.microsoft.com/microsoft-edge/webview2\n" +
                "  4) 重启电脑。\n\n" +
                "是否改用系统默认浏览器打开页面（临时可用）？",
                "WebView2 启动失败",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch { /* ignore */ }
            }
            return;
        }

        webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

        // Show the browser once navigation completes.
        webView.NavigationCompleted += (_, args) =>
        {
            if (args.IsSuccess)
            {
                webView.Visibility = Visibility.Visible;
            }
        };

        webView.Visibility = Visibility.Visible;
        webView.Source = new Uri(url);
    }

    // ---------- Elevation detection (avoids elevated Edge conflict) ----------

    /// <summary>Returns the installed WebView2 Evergreen Runtime version, or "?" if unavailable.</summary>
    private static string GetWebView2RuntimeVersion()
    {
        try
        {
            return Microsoft.Web.WebView2.Core.CoreWebView2Environment
                .GetAvailableBrowserVersionString();
        }
        catch
        {
            return "?";
        }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(
        IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle, int tokenInformationClass, IntPtr tokenInformation,
        uint tokenInformationLength, out uint returnLength);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    private const int TokenElevation = 20;
    private const uint TokenQuery = 0x0008;

    /// <summary>True if the current process is running with admin (elevated) rights.</summary>
    private static bool IsElevated
    {
        get
        {
            using var proc = Process.GetCurrentProcess();
            if (!OpenProcessToken(proc.Handle, TokenQuery, out IntPtr token)) return false;
            try
            {
                int size = Marshal.SizeOf<int>();
                IntPtr buf = Marshal.AllocHGlobal(size);
                try
                {
                    if (!GetTokenInformation(token, TokenElevation, buf, (uint)size, out _))
                        return false;
                    return Marshal.ReadInt32(buf) != 0;
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }
            }
            finally
            {
                CloseHandle(token);
            }
        }
    }

    /// <summary>
    /// Restarts this exe through Explorer so it runs de-elevated (normal user),
    /// which keeps WebView2/Edge in normal privilege mode.
    /// </summary>
    private static void RelaunchAsNormalUser()
    {
        try
        {
            var explorer = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(explorer, "explorer.exe"),
                Arguments = "\"" + Environment.ProcessPath + "\"",
                UseShellExecute = true,
            };
            Process.Start(psi);
        }
        catch
        {
            // If we cannot relaunch, still shut down so we do not run elevated.
        }
    }
}