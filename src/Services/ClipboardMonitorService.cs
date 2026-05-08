using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Interop;
using ClipPanda.Models;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using DataFormats = System.Windows.DataFormats;

namespace ClipPanda.Services;

/// <summary>
/// 剪贴板监听服务
/// </summary>
public class ClipboardMonitorService : IDisposable
{
    #region Win32 API

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    private const int WM_CLIPBOARDUPDATE = 0x031D;

    #endregion

    private readonly DatabaseService _databaseService;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _isMonitoring;
    private readonly bool _enableDeduplication;
    
    // 用于防止并发处理的信号量
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
    // 用于队列化剪贴板更新事件
    private readonly Queue<ClipboardItem> _pendingItems = new();
    private readonly object _queueLock = new();
    private bool _isProcessing = false;

    /// <summary>
    /// 当剪贴板内容变化时触发
    /// </summary>
    public event Action<ClipboardItem>? ClipboardChanged;

    /// <summary>
    /// 当发生错误时触发
    /// </summary>
    public event Action<Exception>? ErrorOccurred;

    public ClipboardMonitorService(DatabaseService databaseService, bool enableDeduplication = true)
    {
        _databaseService = databaseService;
        _enableDeduplication = enableDeduplication;
    }

    /// <summary>
    /// 开始监听剪贴板
    /// </summary>
    public void Start()
    {
        if (_isMonitoring) return;

        var parameters = new HwndSourceParameters("ClipboardMonitorWindow")
        {
            HwndSourceHook = WndProc,
            WindowStyle = 0 // 无边框不可见窗口
        };
        _hwndSource = new HwndSource(parameters);
        _hwnd = _hwndSource.Handle;

        // 设置窗口样式：工具窗口 + 不激活，使其不在任务栏显示
        int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
        SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

        if (!AddClipboardFormatListener(_hwnd))
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to register clipboard listener. Error code: {error}");
        }

        _isMonitoring = true;
    }

    /// <summary>
    /// 停止监听剪贴板
    /// </summary>
    public void Stop()
    {
        if (!_isMonitoring) return;

        if (_hwnd != IntPtr.Zero)
        {
            RemoveClipboardFormatListener(_hwnd);
        }

        _hwndSource?.Dispose();
        _hwndSource = null;
        _isMonitoring = false;
        _processingSemaphore.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_CLIPBOARDUPDATE)
        {
            handled = true;
            // 使用 Dispatcher 确保在 UI 线程捕获剪贴板内容
            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var item = CaptureClipboardContent();
                if (item != null)
                {
                    await EnqueueAndProcessAsync(item);
                }
            });
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 将项目加入队列并触发处理
    /// </summary>
    private async Task EnqueueAndProcessAsync(ClipboardItem item)
    {
        lock (_queueLock)
        {
            _pendingItems.Enqueue(item);
            if (_isProcessing) return;
            _isProcessing = true;
        }

        // 使用信号量确保只有一个处理任务在运行
        await _processingSemaphore.WaitAsync();
        try
        {
            await ProcessQueueAsync();
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    /// <summary>
    /// 处理队列中的所有项目
    /// </summary>
    private async Task ProcessQueueAsync()
    {
        while (true)
        {
            ClipboardItem? item;
            lock (_queueLock)
            {
                if (_pendingItems.Count == 0)
                {
                    _isProcessing = false;
                    return;
                }
                item = _pendingItems.Dequeue();
            }

            try
            {
                await ProcessSingleItemAsync(item);
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(ex);
            }
        }
    }

    /// <summary>
    /// 处理单个剪贴板项目
    /// </summary>
    private async Task ProcessSingleItemAsync(ClipboardItem item)
    {
        if (_enableDeduplication && !string.IsNullOrEmpty(item.ContentHash))
        {
            var existingItem = await _databaseService.FindByHashAsync(item.ContentHash);
            if (existingItem != null)
            {
                existingItem.CopyTime = DateTime.Now;
                await _databaseService.UpdateItemAsync(existingItem);
                ClipboardChanged?.Invoke(existingItem);
                return;
            }
        }

        await _databaseService.AddItemAsync(item);
        ClipboardChanged?.Invoke(item);
    }

    private ClipboardItem? CaptureClipboardContent()
    {
        try
        {
            if (!Clipboard.ContainsData(DataFormats.UnicodeText) &&
                !Clipboard.ContainsData(DataFormats.Text) &&
                !Clipboard.ContainsData(DataFormats.Html) &&
                !Clipboard.ContainsData(DataFormats.Rtf) &&
                !Clipboard.ContainsData(DataFormats.Bitmap) &&
                !Clipboard.ContainsFileDropList())
            {
                return null;
            }

            var item = new ClipboardItem
            {
                CopyTime = DateTime.Now,
                UseCount = 0,
                IsFavorited = false
            };

            if (Clipboard.ContainsData(DataFormats.UnicodeText) || Clipboard.ContainsData(DataFormats.Text))
            {
                var text = Clipboard.GetText();
                item.ContentType = ContentType.Text;
                item.TextContent = text;
                item.Preview = GeneratePreview(text);
                item.ContentHash = ComputeHash(text);

                if (Clipboard.ContainsData(DataFormats.Html))
                {
                    item.ContentType = ContentType.Html;
                }
                else if (Clipboard.ContainsData(DataFormats.Rtf))
                {
                    item.ContentType = ContentType.Rtf;
                }
            }
            else if (Clipboard.ContainsData(DataFormats.Bitmap))
            {
                var image = Clipboard.GetImage();
                if (image != null)
                {
                    item.ContentType = ContentType.Image;
                    item.BinaryContent = ImageToByteArray(image);
                    item.Preview = "[图片]";
                    item.ContentHash = ComputeHash(item.BinaryContent);
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                if (files.Count > 0)
                {
                    item.ContentType = ContentType.Files;
                    item.TextContent = string.Join(Environment.NewLine, files.Cast<string>());
                    item.Preview = files.Count == 1
                        ? $"[文件] {Path.GetFileName(files[0])}"
                        : $"[文件] {files.Count} 个文件";
                    item.ContentHash = ComputeHash(item.TextContent);
                }
            }

            return string.IsNullOrEmpty(item.Preview) ? null : item;
        }
        catch
        {
            return null;
        }
    }

    private static string GeneratePreview(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var preview = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\t", " ");
        while (preview.Contains("  "))
        {
            preview = preview.Replace("  ", " ");
        }

        return preview.Trim();
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static string ComputeHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash);
    }

    private static byte[] ImageToByteArray(System.Windows.Media.Imaging.BitmapSource bitmap)
    {
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public void Dispose()
    {
        Stop();
    }
}
