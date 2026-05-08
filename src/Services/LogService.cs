using Serilog;
using System.IO;

namespace ClipPanda.Services;

/// <summary>
/// 日志服务 - 使用 Serilog
/// </summary>
public static class LogService
{
    private static ILogger? _logger;
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClipPanda", "Logs");

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    public static void Initialize()
    {
        Directory.CreateDirectory(LogDirectory);

        var logFilePath = Path.Combine(LogDirectory, "clipPanda-.log");

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                logFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Info("日志系统初始化完成");
    }

    public static ILogger Logger => _logger ?? throw new InvalidOperationException("日志系统未初始化");

    public static void Debug(string message) => Logger.Debug(message);
    public static void Info(string message) => Logger.Information(message);
    public static void Warning(string message) => Logger.Warning(message);
    public static void Error(string message) => Logger.Error(message);
    public static void Error(Exception ex, string message) => Logger.Error(ex, message);
    public static void Fatal(string message) => Logger.Fatal(message);
    public static void Fatal(Exception ex, string message) => Logger.Fatal(ex, message);

    /// <summary>
    /// 获取日志文件路径
    /// </summary>
    public static string GetLogDirectory() => LogDirectory;
}
