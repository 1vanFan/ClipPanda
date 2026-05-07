using System.Windows;
using System.Windows.Input;
using ClipPanda.Services;
using ClipPanda.ViewModels;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace ClipPanda.Views;

public partial class SettingsView : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsService _settingsService;

    public SettingsView(SettingsService settingsService)
    {
        InitializeComponent();

        _settingsService = settingsService;
        _viewModel = new SettingsViewModel(settingsService);
        DataContext = _viewModel;

        HotkeyTextBox.PreviewKeyDown += HotkeyTextBox_PreviewKeyDown;
    }

    /// <summary>
    /// 快捷键输入捕获
    /// </summary>
    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var keys = new List<string>();

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            keys.Add("Ctrl");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            keys.Add("Shift");
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            keys.Add("Alt");

        if (e.Key != Key.LeftCtrl && e.Key != Key.RightCtrl &&
            e.Key != Key.LeftShift && e.Key != Key.RightShift &&
            e.Key != Key.LeftAlt && e.Key != Key.RightAlt &&
            e.Key != Key.System)
        {
            string keyName = e.Key switch
            {
                Key.Oem3 => "`",
                Key.Oem2 => "/",
                Key.Oem5 => "\\",
                Key.OemPeriod => ".",
                Key.OemComma => ",",
                Key.OemMinus => "-",
                Key.OemPlus => "=",
                Key.Space => "Space",
                Key.Enter => "Enter",
                Key.Tab => "Tab",
                Key.Escape => "Esc",
                Key.Delete => "Delete",
                Key.Back => "Backspace",
                _ => e.Key.ToString()
            };
            keys.Add(keyName);
        }

        if (keys.Count >= 2)
        {
            _viewModel.MainHotkey = string.Join("+", keys);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Save();
        MessageBox.Show("设置已保存", "ClipPanda",
            MessageBoxButton.OK, MessageBoxImage.Information);
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
