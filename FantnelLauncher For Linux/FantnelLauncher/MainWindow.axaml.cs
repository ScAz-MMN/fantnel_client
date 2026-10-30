using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace FantnelLauncher;

public partial class MainWindow : Window
{
    // Frontend dev-server URLs to try, in order (IPv4 first, like the original).
    private static readonly string[] NodeUrlCandidates =
    {
        "http://127.0.0.1:5173",
        "http://localhost:5173",
    };

    // Ports our components listen on (used to kill stale processes before start).
    private static readonly int[] FantnelPorts = { 5173, 3939, 13521 };

    // Max time (seconds) to wait for the frontend dev server to come up.
    private const int WaitForUrlSeconds = 45;

    // BaseDirectory == folder containing the launcher (and st.sh / back / web).
    private static readonly string AppDir = AppContext.BaseDirectory;

    private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

    // Maximize / restore bookkeeping (keeps the taskbar visible).
    private bool _isMaximized;
    private PixelPoint _normalPosition;
    private Size _normalSize;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;

        // Configure the embedded web view on Linux (WebKitGTK backend).
        Web.EnvironmentRequested += (_, e) => e.EnableDevTools = true;
        Web.NavigationCompleted += OnNavigationCompleted;
    }

    // ---------------------------------------------------------------
    //  Startup pipeline (mirrors the original WPF launcher)
    // ---------------------------------------------------------------
    private async void OnOpened(object? sender, EventArgs e)
    {
        Log("launcher started");

        // 1. Check node.js
        if (!NodeJsInstalled())
        {
            Log("node.js is NOT installed");
            ShowMessage("Node.js 未安装。请先安装 Node.js 后再启动。");
            return;
        }
        Log("node.js found");

        // 2. Kill stale processes that may still hold our ports
        KillProcessesOnPorts();
        Log("stale processes cleared");

        // 3. Launch components via st.sh (separate terminal windows)
        RunStSh();
        Log("st.sh launched");

        // 4. Wait until a frontend dev-server URL is reachable
        string? reachable = await FindReachableUrlAsync(NodeUrlCandidates, WaitForUrlSeconds);
        if (reachable == null)
        {
            Log("frontend did not become reachable in time");
            ShowMessage(
                $"前端服务在 {WaitForUrlSeconds} 秒内未就绪。\n" +
                "请确认 Node.js 已安装、web 目录下已执行 npm install。");
            return;
        }
        Log("frontend ready: " + reachable);

        // 5. Point the embedded web view at it
        try
        {
            Web.Source = new Uri(reachable);
            Log("web view navigated to " + reachable);
        }
        catch (Exception ex)
        {
            Log("failed to load web view: " + ex.Message);
            OpenSystemBrowser(reachable);
        }
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            Dispatcher.UIThread.Post(() => Web.IsVisible = true);
            Log("web view loaded OK: " + e.Request);
        }
        else
        {
            Log("web view navigation FAILED: " + e.Request);
        }
    }

    // ---------------------------------------------------------------
    //  Node.js check
    // ---------------------------------------------------------------
    private static bool NodeJsInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo("node", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            string? ver = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return !string.IsNullOrWhiteSpace(ver) && ver.StartsWith("v");
        }
        catch
        {
            return false;
        }
    }

    // ---------------------------------------------------------------
    //  Kill processes that listen on our ports
    // ---------------------------------------------------------------
    private static void KillProcessesOnPorts()
    {
        var wanted = new HashSet<int>(FantnelPorts);
        var pids = new HashSet<int>();

        // Map listening ports to owning PIDs using `ss` (Linux).
        try
        {
            var psi = new ProcessStartInfo("ss", "-ltnp")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                foreach (var line in output.Split('\n'))
                {
                    // Only care about lines for our ports, e.g. ":5173"
                    int port = ParsePortFromSsLine(line);
                    if (port >= 0 && wanted.Contains(port))
                    {
                        var pid = ParsePidFromSsLine(line);
                        if (pid > 0) pids.Add(pid);
                    }
                }
            }
        }
        catch { /* ss may not exist */ }

        // Kill the stale processes we found.
        foreach (var pid in pids)
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                proc.Kill();
                proc.WaitForExit(3000);
                Log($"killed stale {proc.ProcessName} (pid {pid})");
            }
            catch { /* already gone or not ours */ }
        }
    }

    private static int ParsePortFromSsLine(string line)
    {
        // Lines look like: LISTEN 0 511 127.0.0.1:5173 0.0.0.0:* users:(("node",pid=123,fd=15))
        var idx = line.IndexOf(':');
        if (idx < 0) return -1;
        var rest = line.Substring(idx + 1);
        var end = 0;
        while (end < rest.Length && char.IsDigit(rest[end])) end++;
        if (end == 0) return -1;
        return int.TryParse(rest.Substring(0, end), out var port) ? port : -1;
    }

    private static int ParsePidFromSsLine(string line)
    {
        var marker = "pid=";
        var idx = line.IndexOf(marker);
        if (idx < 0) return -1;
        var rest = line.Substring(idx + marker.Length);
        var end = 0;
        while (end < rest.Length && char.IsDigit(rest[end])) end++;
        if (end == 0) return -1;
        return int.TryParse(rest.Substring(0, end), out var pid) ? pid : -1;
    }

    // ---------------------------------------------------------------
    //  Launch st.sh silently (no console window from the launcher)
    // ---------------------------------------------------------------
    private static void RunStSh()
    {
        var stSh = Path.Combine(AppDir, "st.sh");
        if (!File.Exists(stSh))
        {
            Log("st.sh NOT found at " + stSh);
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"\"{stSh}\"",
            WorkingDirectory = AppDir,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log("failed to start st.sh: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------
    //  Poll the frontend until one candidate responds
    // ---------------------------------------------------------------
    private async Task<string?> FindReachableUrlAsync(string[] candidates, int timeoutSeconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var url in candidates)
            {
                try
                {
                    var resp = await _http.GetAsync(url);
                    if (resp.IsSuccessStatusCode)
                        return url;
                }
                catch { /* not up yet */ }
            }
            await Task.Delay(500);
        }
        return null;
    }

    // ---------------------------------------------------------------
    //  System-browser fallback (xdg-open on Linux)
    // ---------------------------------------------------------------
    private static void OpenSystemBrowser(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Log("failed to open system browser: " + ex.Message);
        }
    }

    // ---------------------------------------------------------------
    //  MAC-style title bar: drag / maximize buttons
    // ---------------------------------------------------------------
    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void MacButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string tag)
        {
            switch (tag)
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
    }

    private void ToggleMaximize()
    {
        if (!_isMaximized)
        {
            _normalPosition = Position;
            _normalSize = new Size(Width, Height);
        }

        var area = Screens.Primary?.WorkingArea;
        if (area == null)
        {
            WindowState = _isMaximized ? WindowState.Normal : WindowState.Maximized;
            _isMaximized = !_isMaximized;
            return;
        }

        if (_isMaximized)
        {
            // Restore
            Position = _normalPosition;
            Width = _normalSize.Width;
            Height = _normalSize.Height;
            _isMaximized = false;
        }
        else
        {
            // Maximize to the working area, keeping the taskbar visible
            Position = area.Value.Position;
            Width = area.Value.Width;
            Height = area.Value.Height;
            _isMaximized = true;
        }
    }

    // ---------------------------------------------------------------
    //  Simple modal message box (Avalonia has no built-in MessageBox)
    // ---------------------------------------------------------------
    private void ShowMessage(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var msg = new Window
            {
                Title = "Fantnel",
                Width = 440,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
            };
            var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16 };
            panel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 400,
            });
            var ok = new Button
            {
                Content = "确定",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            ok.Click += (_, _) => msg.Close();
            panel.Children.Add(ok);
            msg.Content = panel;
            msg.ShowDialog(this);
        });
    }

    // ---------------------------------------------------------------
    //  Logging
    // ---------------------------------------------------------------
    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}" + Environment.NewLine;
        string[] paths =
        {
            Path.Combine(AppDir, "launcher.log"),
            Path.Combine(Path.GetTempPath(), "FantnelLauncher", "launcher.log"),
        };
        foreach (var path in paths)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path, line);
            }
            catch { /* best-effort */ }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _http.Dispose();
        base.OnClosed(e);
    }
}