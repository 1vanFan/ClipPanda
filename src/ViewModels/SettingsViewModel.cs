using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClipPanda.Models;
using ClipPanda.Services;

namespace ClipPanda.ViewModels;

/// <summary>
/// 设置窗口视图模型
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly SettingsService _settingsService;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var s = _settingsService.Settings;
        MaxHistoryCount = s.MaxHistoryCount;
        HistoryRetentionDays = s.HistoryRetentionDays;
        StartWithWindows = s.StartWithWindows;
        MainHotkey = s.MainHotkey;
        EnableAutoDeduplication = s.EnableAutoDeduplication;
        EnableSensitiveContentDetection = s.EnableSensitiveContentDetection;
        MinimizeToTray = s.MinimizeToTray;
        ShowCopyNotification = s.ShowCopyNotification;
        DarkModeOption = s.UseDarkMode switch
        {
            true => "Dark",
            false => "Light",
            _ => "Auto"
        };
    }

    private int _maxHistoryCount;
    public int MaxHistoryCount
    {
        get => _maxHistoryCount;
        set { _maxHistoryCount = value; OnPropertyChanged(); }
    }

    private int _historyRetentionDays;
    public int HistoryRetentionDays
    {
        get => _historyRetentionDays;
        set { _historyRetentionDays = value; OnPropertyChanged(); }
    }

    private bool _startWithWindows;
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { _startWithWindows = value; OnPropertyChanged(); }
    }

    private string _mainHotkey = "Ctrl+Shift+C";
    public string MainHotkey
    {
        get => _mainHotkey;
        set { _mainHotkey = value; OnPropertyChanged(); }
    }

    private bool _enableAutoDeduplication;
    public bool EnableAutoDeduplication
    {
        get => _enableAutoDeduplication;
        set { _enableAutoDeduplication = value; OnPropertyChanged(); }
    }

    private bool _enableSensitiveContentDetection;
    public bool EnableSensitiveContentDetection
    {
        get => _enableSensitiveContentDetection;
        set { _enableSensitiveContentDetection = value; OnPropertyChanged(); }
    }

    private bool _minimizeToTray;
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set { _minimizeToTray = value; OnPropertyChanged(); }
    }

    private bool _showCopyNotification;
    public bool ShowCopyNotification
    {
        get => _showCopyNotification;
        set { _showCopyNotification = value; OnPropertyChanged(); }
    }

    private string _darkModeOption = "Auto";
    public string DarkModeOption
    {
        get => _darkModeOption;
        set
        {
            _darkModeOption = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void Save()
    {
        _settingsService.Update(s =>
        {
            s.MaxHistoryCount = MaxHistoryCount;
            s.HistoryRetentionDays = HistoryRetentionDays;
            s.StartWithWindows = StartWithWindows;
            s.MainHotkey = MainHotkey;
            s.EnableAutoDeduplication = EnableAutoDeduplication;
            s.EnableSensitiveContentDetection = EnableSensitiveContentDetection;
            s.MinimizeToTray = MinimizeToTray;
            s.ShowCopyNotification = ShowCopyNotification;
            s.UseDarkMode = DarkModeOption switch
            {
                "Dark" => true,
                "Light" => false,
                _ => null
            };
        });

        // 应用开机自启动设置
        try
        {
            StartupService.SetStartup(StartWithWindows);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"设置开机自启动失败: {ex.Message}", "错误", 
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        // 应用深色模式主题
        bool? useDarkMode = DarkModeOption switch
        {
            "Dark" => true,
            "Light" => false,
            _ => null
        };
        ThemeService.ApplyTheme("Blue", useDarkMode);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
