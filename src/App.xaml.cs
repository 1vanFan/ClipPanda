using System.Drawing;
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

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            System.Diagnostics.Debug.WriteLine("[ClipPanda] 应用程序启动中...");

            // 初始化设置服务
            _settingsService = new SettingsService();

            // 应用主题
            ThemeService.ApplyTheme(_settingsService.Settings.ThemeColor);

            // 初始化数据库
            _databaseService = new DatabaseService();

            // 初始化快捷键服务
            _hotkeyService = new HotkeyService();
            _hotkeyService.Initialize();

            // 初始化剪贴板监听
            _clipboardMonitor = new ClipboardMonitorService(
                _databaseService,
                enableDeduplication: _settingsService.Settings.EnableAutoDeduplication);
            _clipboardMonitor.Start();

            // 订阅错误事件
            _clipboardMonitor.ErrorOccurred += OnServiceError;
            _hotkeyService.ErrorOccurred += OnServiceError;

            System.Diagnostics.Debug.WriteLine("[ClipPanda] 服务初始化完成");

            // 创建主窗口（初始不显示）
            _mainWindow = new MainWindow(_databaseService, _clipboardMonitor, _hotkeyService, _settingsService);
            System.Diagnostics.Debug.WriteLine("[ClipPanda] 主窗口创建完成");

            // 创建系统托盘图标
            CreateTaskbarIcon();
            System.Diagnostics.Debug.WriteLine("[ClipPanda] 系统托盘图标创建完成");

            // 注册主快捷键
            RegisterMainHotkey();

            // 启动时清理过期记录
            _ = CleanupExpiredItemsAsync();

            System.Diagnostics.Debug.WriteLine("[ClipPanda] 应用程序启动完成");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClipPanda] 启动错误: {ex}");
            MessageBox.Show($"应用程序启动失败:\n{ex.Message}\n\n{ex.StackTrace}",
                "ClipPanda 错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
            MessageBox.Show($"无法注册快捷键 {hotkey}，可能已被其他程序占用。\n请在设置中更改快捷键。",
                "ClipPanda", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[ClipPanda] 快捷键 {hotkey} 注册成功");
        }
    }

    /// <summary>
    /// 创建系统托盘图标
    /// </summary>
    private void CreateTaskbarIcon()
    {
        var icon = CreateSimpleIcon();

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
    /// 创建简单的图标 - 熊猫剪贴板
    /// </summary>
    private Icon CreateSimpleIcon()
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
            var count = await _databaseService.CleanupExpiredItemsAsync(_settingsService.Settings.HistoryRetentionDays);
            if (count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"已清理 {count} 条过期记录");
            }
        }
    }

    /// <summary>
    /// 服务错误处理
    /// </summary>
    private void OnServiceError(Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            System.Diagnostics.Debug.WriteLine($"服务错误: {ex.Message}");
        });
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _databaseService?.Dispose();
        _taskbarIcon?.Dispose();

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
