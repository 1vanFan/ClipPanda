using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;

namespace ClipPanda.Services;

/// <summary>
/// 全局异常处理器
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// 初始化全局异常处理
    /// </summary>
    public static void Initialize()
    {
        // 捕获UI线程异常
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 捕获非UI线程异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 捕获Task异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        LogService.Info("全局异常处理器初始化完成");
    }

    /// <summary>
    /// UI线程未处理异常
    /// </summary>
    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogService.Error(e.Exception, "UI线程未处理异常");

        var result = MessageBox.Show(
            $"发生错误: {e.Exception.Message}\n\n是否继续运行?",
            "ClipPanda - 错误",
            MessageBoxButton.YesNo,
            MessageBoxImage.Error);

        if (result == MessageBoxResult.Yes)
        {
            e.Handled = true;
        }
        else
        {
            Application.Current.Shutdown(1);
        }
    }

    /// <summary>
    /// 非UI线程未处理异常
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        LogService.Fatal(exception, "未处理异常 - 程序即将终止");

        MessageBox.Show(
            $"发生严重错误，程序即将关闭。\n\n错误: {exception?.Message}\n\n日志位置: {LogService.GetLogDirectory()}",
            "ClipPanda - 致命错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    /// <summary>
    /// Task未观察到的异常
    /// </summary>
    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogService.Error(e.Exception, "Task未观察到的异常");
        e.SetObserved();
    }

    /// <summary>
    /// 包装操作并捕获异常
    /// </summary>
    public static void SafeExecute(Action action, string? context = null)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            var message = context != null ? $"[{context}] {ex.Message}" : ex.Message;
            LogService.Error(ex, message);
            throw;
        }
    }

    /// <summary>
    /// 异步包装操作并捕获异常
    /// </summary>
    public static async Task SafeExecuteAsync(Func<Task> action, string? context = null)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            var message = context != null ? $"[{context}] {ex.Message}" : ex.Message;
            LogService.Error(ex, message);
            throw;
        }
    }
}
