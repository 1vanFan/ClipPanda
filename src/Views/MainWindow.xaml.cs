using System.Windows;
using System.Windows.Input;
using ClipPanda.Models;
using ClipPanda.Services;
using ClipPanda.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;
using FormsScreen = System.Windows.Forms.Screen;

namespace ClipPanda.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DatabaseService _databaseService;
    private readonly SettingsService _settingsService;
    private bool _isDragging = false;
    private Point _dragStartPoint;

    public MainWindow(DatabaseService databaseService, ClipboardMonitorService clipboardMonitor, HotkeyService hotkeyService, SettingsService settingsService)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _settingsService = settingsService;
        _viewModel = new MainViewModel(databaseService);
        DataContext = _viewModel;

        // 订阅剪贴板变化事件
        clipboardMonitor.ClipboardChanged += OnClipboardChanged;

        Loaded += MainWindow_Loaded;
        LocationChanged += MainWindow_LocationChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus();
        PositionWindow();
    }

    /// <summary>
    /// 窗口位置变化时保存位置
    /// </summary>
    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (_settingsService.Settings.RememberWindowPosition && IsVisible && WindowState == WindowState.Normal)
        {
            _settingsService.Update(s =>
            {
                s.WindowLeft = Left;
                s.WindowTop = Top;
                // 获取当前窗口所在的屏幕
                var currentScreen = GetCurrentScreen();
                if (currentScreen != null)
                {
                    s.WindowScreenDeviceName = currentScreen.DeviceName;
                }
            });
        }
    }

    /// <summary>
    /// 获取窗口当前所在的屏幕
    /// </summary>
    private FormsScreen? GetCurrentScreen()
    {
        var windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        return FormsScreen.FromHandle(windowHandle);
    }

    /// <summary>
    /// 定位窗口 - 支持多屏和位置记忆
    /// </summary>
    private void PositionWindow()
    {
        var settings = _settingsService.Settings;

        // 如果有记住的位置，尝试恢复到对应屏幕
        if (settings.RememberWindowPosition && settings.WindowLeft >= 0 && settings.WindowTop >= 0)
        {
            // 查找之前保存的屏幕
            var targetScreen = FindScreenByDeviceName(settings.WindowScreenDeviceName);
            
            if (targetScreen != null)
            {
                // 恢复到之前屏幕的相对位置
                Left = settings.WindowLeft;
                Top = settings.WindowTop;
                
                // 确保窗口在屏幕可见区域内
                EnsureWindowVisible(targetScreen);
                return;
            }
        }

        // 默认居中显示（Raycast风格）
        CenterWindowOnPrimaryScreen();
    }

    /// <summary>
    /// 根据设备名查找屏幕
    /// </summary>
    private FormsScreen? FindScreenByDeviceName(string? deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return null;

        return FormsScreen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
    }

    /// <summary>
    /// 确保窗口在屏幕可见区域内
    /// </summary>
    private void EnsureWindowVisible(FormsScreen screen)
    {
        var workingArea = screen.WorkingArea;
        
        // 如果窗口超出屏幕边界，调整到可见区域
        if (Left + ActualWidth > workingArea.Right)
            Left = workingArea.Right - ActualWidth;
        if (Left < workingArea.Left)
            Left = workingArea.Left;
        if (Top + ActualHeight > workingArea.Bottom)
            Top = workingArea.Bottom - ActualHeight;
        if (Top < workingArea.Top)
            Top = workingArea.Top;
    }

    /// <summary>
    /// 在主屏幕中央显示（Raycast风格）
    /// </summary>
    private void CenterWindowOnPrimaryScreen()
    {
        var primaryScreen = FormsScreen.PrimaryScreen;
        if (primaryScreen != null)
        {
            var workingArea = primaryScreen.WorkingArea;
            Left = (workingArea.Width - Width) / 2 + workingArea.Left;
            Top = (workingArea.Height - Height) / 2 + workingArea.Top;
        }
    }

    /// <summary>
    /// 在当前鼠标所在的屏幕中央显示
    /// </summary>
    private void CenterWindowOnCurrentScreen()
    {
        var currentScreen = FormsScreen.FromPoint(System.Windows.Forms.Cursor.Position);
        if (currentScreen != null)
        {
            var workingArea = currentScreen.WorkingArea;
            Left = (workingArea.Width - Width) / 2 + workingArea.Left;
            Top = (workingArea.Height - Height) / 2 + workingArea.Top;
        }
    }

    /// <summary>
    /// 切换窗口可见性
    /// </summary>
    public void ToggleVisibility()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            // 每次显示时重新定位（跟随当前鼠标所在屏幕）
            CenterWindowOnCurrentScreen();
            Show();
            Activate();
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }
    }

    /// <summary>
    /// 标题栏鼠标按下 - 开始拖动
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // 双击标题栏可以最大化/还原（可选）
            // WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
        else
        {
            // 开始拖动
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            DragMove();
        }
    }

    /// <summary>
    /// 剪贴板内容变化处理
    /// </summary>
    private void OnClipboardChanged(ClipboardItem item)
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.RefreshItems();
        });
    }

    /// <summary>
    /// 窗口失去焦点时隐藏
    /// </summary>
    private void MainWindow_OnDeactivated(object? sender, EventArgs e)
    {
        // 可选：失去焦点时自动隐藏
    }

    /// <summary>
    /// Tab 切换 - 全部
    /// </summary>
    private void AllTab_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowFavoritesOnly = false;
    }

    /// <summary>
    /// Tab 切换 - 收藏
    /// </summary>
    private void FavoritesTab_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ShowFavoritesOnly = true;
    }

    /// <summary>
    /// 搜索框文本变化
    /// </summary>
    private void SearchTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.SearchText = SearchTextBox.Text;
        _viewModel.RefreshItems();
    }

    /// <summary>
    /// 列表双击粘贴
    /// </summary>
    private void ClipboardListBox_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        PasteSelectedItem();
    }

    /// <summary>
    /// 键盘快捷键处理
    /// </summary>
    private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Enter:
                PasteSelectedItem();
                e.Handled = true;
                break;

            case Key.Delete:
                DeleteSelectedItem();
                e.Handled = true;
                break;

            case Key.S when Keyboard.Modifiers == ModifierKeys.Control:
                ToggleFavoriteSelectedItem();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// 粘贴选中项
    /// </summary>
    private void PasteSelectedItem()
    {
        if (_viewModel.SelectedItem != null)
        {
            var item = _viewModel.SelectedItem.Item;

            if (item.ContentType == ContentType.Text ||
                item.ContentType == ContentType.Html ||
                item.ContentType == ContentType.Rtf ||
                item.ContentType == ContentType.Files)
            {
                Clipboard.SetText(item.TextContent ?? string.Empty);
            }
            else if (item.ContentType == ContentType.Image && item.BinaryContent != null)
            {
                // 图片粘贴需要转换为 BitmapSource
            }

            _ = _databaseService.IncrementUseCountAsync(item.Id);

            Hide();
            SendPasteKey();
        }
    }

    /// <summary>
    /// 发送粘贴按键
    /// </summary>
    private void SendPasteKey()
    {
        System.Windows.Forms.SendKeys.SendWait("^v");
    }

    /// <summary>
    /// 删除选中项
    /// </summary>
    private void DeleteSelectedItem()
    {
        if (_viewModel.SelectedItem != null)
        {
            _ = _databaseService.DeleteItemAsync(_viewModel.SelectedItem.Id);
            _viewModel.RefreshItems();
        }
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    private async void ToggleFavoriteSelectedItem()
    {
        if (_viewModel.SelectedItem != null)
        {
            var item = _viewModel.SelectedItem.Item;
            item.IsFavorited = !item.IsFavorited;
            await _databaseService.UpdateItemAsync(item);
            _viewModel.RefreshItems();
        }
    }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    /// <summary>
    /// 设置按钮
    /// </summary>
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsView(_settingsService)
        {
            Owner = this
        };
        settingsWindow.ShowDialog();
    }
}
