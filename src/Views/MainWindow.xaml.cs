using System.Windows;
using System.Windows.Input;
using ClipPanda.Models;
using ClipPanda.Services;
using ClipPanda.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;
using MessageBox = System.Windows.MessageBox;
using Clipboard = System.Windows.Clipboard;

namespace ClipPanda.Views;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DatabaseService _databaseService;

    public MainWindow(DatabaseService databaseService, ClipboardMonitorService clipboardMonitor, HotkeyService hotkeyService)
    {
        InitializeComponent();

        _databaseService = databaseService;
        _viewModel = new MainViewModel(databaseService);
        DataContext = _viewModel;

        // 订阅剪贴板变化事件
        clipboardMonitor.ClipboardChanged += OnClipboardChanged;

        // 注册快捷键
        hotkeyService.RegisterHotkey("Ctrl+`", ToggleVisibility);

        // 初始隐藏窗口
        Hide();

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 加载完成后聚焦到搜索框
        SearchTextBox.Focus();

        // 居中窗口
        CenterWindow();
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
            Show();
            Activate();
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
            CenterWindow();
        }
    }

    /// <summary>
    /// 窗口居中
    /// </summary>
    private void CenterWindow()
    {
        var screen = System.Windows.Forms.Screen.FromHandle(
            new System.Windows.Interop.WindowInteropHelper(this).Handle);

        Left = (screen.WorkingArea.Width - ActualWidth) / 2 + screen.WorkingArea.Left;
        Top = (screen.WorkingArea.Height - ActualHeight) / 2 + screen.WorkingArea.Top;
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

            // 设置剪贴板内容
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

            // 更新使用次数
            _ = _databaseService.IncrementUseCountAsync(item.Id);

            // 模拟粘贴操作（发送 Ctrl+V）
            Hide();
            SendPasteKey();
        }
    }

    /// <summary>
    /// 发送粘贴按键
    /// </summary>
    private void SendPasteKey()
    {
        // 使用 SendKeys 发送 Ctrl+V
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
        MessageBox.Show("设置功能将在后续版本中提供", "ClipPanda", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
