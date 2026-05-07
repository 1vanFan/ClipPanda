using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ClipPanda.Services;

/// <summary>
/// 快捷键注册服务
/// </summary>
public class HotkeyService : IDisposable
{
    #region Win32 API

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    // 修饰键常量
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;

    private const int WM_HOTKEY = 0x0312;

    #endregion

    private readonly Dictionary<int, Action> _hotkeyCallbacks = new();
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private int _currentId;
    private bool _isRunning;

    /// <summary>
    /// 当发生错误时触发
    /// </summary>
    public event Action<Exception>? ErrorOccurred;

    /// <summary>
    /// 初始化服务
    /// </summary>
    public void Initialize()
    {
        if (_isRunning) return;

        var parameters = new HwndSourceParameters("HotkeyWindow")
        {
            HwndSourceHook = WndProc,
            WindowStyle = 0 // 无边框不可见窗口
        };
        _hwndSource = new HwndSource(parameters);
        _hwnd = _hwndSource.Handle;

        // 设置窗口样式：工具窗口 + 不激活，使其不在任务栏显示
        int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        _isRunning = true;
    }

    /// <summary>
    /// 注册全局快捷键
    /// </summary>
    /// <param name="hotkeyString">快捷键字符串，如 "Ctrl+`" 或 "Ctrl+Shift+V"</param>
    /// <param name="callback">回调函数</param>
    /// <returns>是否注册成功</returns>
    public bool RegisterHotkey(string hotkeyString, Action callback)
    {
        if (!_isRunning)
        {
            Initialize();
        }

        var (modifiers, key) = ParseHotkeyString(hotkeyString);
        if (key == 0)
        {
            return false;
        }

        var id = ++_currentId;

        if (!RegisterHotKey(_hwnd, id, modifiers, key))
        {
            var error = Marshal.GetLastWin32Error();
            ErrorOccurred?.Invoke(new InvalidOperationException(
                $"Failed to register hotkey '{hotkeyString}'. Error code: {error}. " +
                "The hotkey may already be in use by another application."));
            return false;
        }

        _hotkeyCallbacks[id] = callback;
        return true;
    }

    /// <summary>
    /// 注销所有快捷键
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var id in _hotkeyCallbacks.Keys.ToList())
        {
            UnregisterHotKey(_hwnd, id);
        }
        _hotkeyCallbacks.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyCallbacks.TryGetValue(id, out var callback))
            {
                handled = true;
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(ex);
                }
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 解析快捷键字符串
    /// </summary>
    private static (uint modifiers, uint key) ParseHotkeyString(string hotkeyString)
    {
        uint modifiers = 0;
        uint key = 0;

        var parts = hotkeyString.Split('+', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToUpperInvariant())
            .ToArray();

        foreach (var part in parts)
        {
            switch (part)
            {
                case "CTRL":
                case "CONTROL":
                    modifiers |= MOD_CONTROL;
                    break;
                case "ALT":
                    modifiers |= MOD_ALT;
                    break;
                case "SHIFT":
                    modifiers |= MOD_SHIFT;
                    break;
                case "WIN":
                case "WINDOWS":
                    modifiers |= MOD_WIN;
                    break;
                default:
                    key = ParseKey(part);
                    break;
            }
        }

        return (modifiers, key);
    }

    /// <summary>
    /// 解析按键
    /// </summary>
    private static uint ParseKey(string key)
    {
        // 特殊按键映射
        return key.ToUpperInvariant() switch
        {
            "`" or "OEM3" => 0xC0,      // ` 键
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "0" => 0x30,
            "1" => 0x31,
            "2" => 0x32,
            "3" => 0x33,
            "4" => 0x34,
            "5" => 0x35,
            "6" => 0x36,
            "7" => 0x37,
            "8" => 0x38,
            "9" => 0x39,
            "A" => 0x41,
            "B" => 0x42,
            "C" => 0x43,
            "D" => 0x44,
            "E" => 0x45,
            "F" => 0x46,
            "G" => 0x47,
            "H" => 0x48,
            "I" => 0x49,
            "J" => 0x4A,
            "K" => 0x4B,
            "L" => 0x4C,
            "M" => 0x4D,
            "N" => 0x4E,
            "O" => 0x4F,
            "P" => 0x50,
            "Q" => 0x51,
            "R" => 0x52,
            "S" => 0x53,
            "T" => 0x54,
            "U" => 0x55,
            "V" => 0x56,
            "W" => 0x57,
            "X" => 0x58,
            "Y" => 0x59,
            "Z" => 0x5A,
            "SPACE" => 0x20,
            "ENTER" or "RETURN" => 0x0D,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "INSERT" => 0x2D,
            "DELETE" => 0x2E,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "UP" => 0x26,
            "DOWN" => 0x28,
            "LEFT" => 0x25,
            "RIGHT" => 0x27,
            _ => 0
        };
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwndSource?.Dispose();
    }
}
