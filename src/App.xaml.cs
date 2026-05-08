using System.Drawing;
using System.Runtime.InteropServices;
using ClipPanda.Services;
using ClipPanda.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace ClipPanda;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    private DatabaseService? _databaseService;
    private ClipboardMonitorService? _clipboardMonitor;
    private HotkeyService? _hotkeyService;
    private SettingsService? _settingsService;
    private MainWindow? _mainWindow;
    private TaskbarIcon? _taskbarIcon;

    // 单实例互斥锁
    private static Mutex? _mutex;
    private const string MutexName = "ClipPanda_SingleInstance_Mutex";

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        // 检查是否已有实例在运行
        _mutex = new Mutex(true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            // 已有实例在运行，激活它并退出
            ActivateExistingInstance();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // 初始化日志系统
        LogService.Initialize();

        // 初始化全局异常处理
        ExceptionHandler.Initialize();

        try
        {
            LogService.Info("应用程序启动中...");

            // 初始化设置服务
            _settingsService = new SettingsService();
            LogService.Info("设置服务初始化完成");

            // 应用主题（使用深色模式设置）
            var useDarkMode = _settingsService.Settings.UseDarkMode;
            ThemeService.ApplyTheme("Blue", useDarkMode);
            LogService.Info($"主题应用: Blue, 深色模式: {useDarkMode?.ToString() ?? "跟随系统"}");

            // 初始化数据库
            _databaseService = new DatabaseService();
            LogService.Info("数据库服务初始化完成");

            // 初始化快捷键服务
            _hotkeyService = new HotkeyService();
            _hotkeyService.Initialize();
            LogService.Info("快捷键服务初始化完成");

            // 初始化剪贴板监听
            _clipboardMonitor = new ClipboardMonitorService(
                _databaseService,
                enableDeduplication: _settingsService.Settings.EnableAutoDeduplication);
            _clipboardMonitor.Start();
            LogService.Info("剪贴板监听服务启动完成");

            // 订阅错误事件
            _clipboardMonitor.ErrorOccurred += OnServiceError;
            _hotkeyService.ErrorOccurred += OnServiceError;

            // 创建主窗口（初始不显示）
            _mainWindow = new MainWindow(_databaseService, _clipboardMonitor, _hotkeyService, _settingsService);
            LogService.Info("主窗口创建完成");

            // 创建系统托盘图标
            CreateTaskbarIcon();
            LogService.Info("系统托盘图标创建完成");

            // 注册主快捷键
            RegisterMainHotkey();

            // 启动时清理过期记录
            _ = CleanupExpiredItemsAsync();

            LogService.Info("应用程序启动完成");
        }
        catch (Exception ex)
        {
            LogService.Fatal(ex, "应用程序启动失败");
            MessageBox.Show($"应用程序启动失败:\n{ex.Message}\n\n日志位置: {LogService.GetLogDirectory()}",
                "ClipPanda 错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 激活已存在的实例窗口
    /// </summary>
    private void ActivateExistingInstance()
    {
        // 查找已存在的窗口
        var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
        foreach (var process in System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName))
        {
            if (process.Id != currentProcess.Id)
            {
                // 激活窗口
                ShowWindow(process.MainWindowHandle, SW_RESTORE);
                SetForegroundWindow(process.MainWindowHandle);
                break;
            }
        }
    }

    /// <summary>
    /// 注册主快捷键
    /// </summary>
    private void RegisterMainHotkey()
    {
        if (_hotkeyService == null || _mainWindow == null || _settingsService == null)
            return;

        var hotkey = _settingsService.Settings.MainHotkey;
        bool hotkeyRegistered = _hotkeyService.RegisterHotkey(hotkey, () =>
        {
            _mainWindow.ToggleVisibility();
        });

        if (!hotkeyRegistered)
        {
            LogService.Warning($"无法注册快捷键 {hotkey}");
            MessageBox.Show($"无法注册快捷键 {hotkey}，可能已被其他程序占用。\n请在设置中更改快捷键。",
                "ClipPanda", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        else
        {
            LogService.Info($"快捷键 {hotkey} 注册成功");
        }
    }

    /// <summary>
    /// 创建系统托盘图标
    /// </summary>
    private void CreateTaskbarIcon()
    {
        // 尝试从 Assets 目录加载图标
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
        System.Drawing.Icon? icon = null;
        
        try
        {
            if (System.IO.File.Exists(iconPath))
            {
                icon = System.Drawing.Icon.ExtractAssociatedIcon(iconPath);
            }
        }
        catch (Exception ex)
        {
            LogService.Warning($"加载图标文件失败: {ex.Message}");
        }
        
        // 如果加载失败，使用备用图标
        icon ??= CreateFallbackIcon();

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "ClipPanda - 剪贴板管理器",
            Icon = icon,

            ContextMenu = new System.Windows.Controls.ContextMenu
            {
                Items =
                {
                    new System.Windows.Controls.MenuItem
                    {
                        Header = "打开主窗口",
                        Command = new RelayCommand(() => _mainWindow?.ToggleVisibility())
                    },
                    new System.Windows.Controls.MenuItem
                    {
                        Header = "设置",
                        Command = new RelayCommand(OpenSettings)
                    },
                    new System.Windows.Controls.Separator(),
                    new System.Windows.Controls.MenuItem
                    {
                        Header = "退出",
                        Command = new RelayCommand(Shutdown)
                    }
                }
            }
        };

        _taskbarIcon.TrayLeftMouseDown += (s, ev) =>
        {
            _mainWindow?.ToggleVisibility();
        };
    }

    /// <summary>
    /// 打开设置窗口
    /// </summary>
    private void OpenSettings()
    {
        if (_settingsService == null || _mainWindow == null) return;

        var settingsWindow = new SettingsView(_settingsService)
        {
            Owner = _mainWindow
        };
        settingsWindow.ShowDialog();
    }

    /// <summary>
    /// 备用图标 - 当 icon.ico 文件不存在时使用
    /// </summary>
    private Icon CreateFallbackIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);

        // 填充背景 - 熊猫黑白色
        graphics.Clear(Color.FromArgb(45, 45, 45));

        // 绘制熊猫耳朵（黑色圆）
        using var earBrush = new SolidBrush(Color.FromArgb(45, 45, 45));
        graphics.FillEllipse(earBrush, 2, 2, 10, 10);
        graphics.FillEllipse(earBrush, 20, 2, 10, 10);

        // 绘制熊猫头部（白色圆形）
        using var headBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(headBrush, 4, 6, 24, 22);

        // 绘制熊猫眼睛（黑色椭圆）
        using var eyeBrush = new SolidBrush(Color.FromArgb(45, 45, 45));
        graphics.FillEllipse(eyeBrush, 8, 12, 6, 7);
        graphics.FillEllipse(eyeBrush, 18, 12, 6, 7);

        // 绘制熊猫眼睛高光（白色小点）
        using var highlightBrush = new SolidBrush(Color.White);
        graphics.FillEllipse(highlightBrush, 10, 13, 2, 2);
        graphics.FillEllipse(highlightBrush, 20, 13, 2, 2);

        // 绘制熊猫鼻子（黑色小椭圆）
        graphics.FillEllipse(eyeBrush, 14, 19, 4, 3);

        // 绘制剪贴板背景（底部）
        using var boardBrush = new SolidBrush(Color.FromArgb(43, 87, 154));
        graphics.FillRectangle(boardBrush, 6, 24, 20, 6);

        // 绘制剪贴板夹子
        using var clipBrush = new SolidBrush(Color.White);
        graphics.FillRectangle(clipBrush, 12, 22, 8, 5);

        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    /// <summary>
    /// 清理过期记录
    /// </summary>
    private async Task CleanupExpiredItemsAsync()
    {
        if (_databaseService != null && _settingsService != null)
        {
            try
            {
                var count = await _databaseService.CleanupExpiredItemsAsync(_settingsService.Settings.HistoryRetentionDays);
                if (count > 0)
                {
                    LogService.Info($"已清理 {count} 条过期记录");
                }
            }
            catch (Exception ex)
            {
                LogService.Error(ex, "清理过期记录失败");
            }
        }
    }

    /// <summary>
    /// 服务错误处理
    /// </summary>
    private void OnServiceError(Exception ex)
    {
        LogService.Error(ex, "服务错误");
        Dispatcher.Invoke(() =>
        {
            // 可选：显示托盘通知
        });
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        LogService.Info("应用程序正在退出...");

        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _databaseService?.Dispose();
        _taskbarIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();

        LogService.Info("应用程序已退出");
        base.OnExit(e);
    }
}

/// <summary>
/// 简单的 RelayCommand 实现
/// </summary>
public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => _execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
