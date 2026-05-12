using Microsoft.Win32;
using System.IO;

namespace ClipPanda.Services;

/// <summary>
/// 开机自启动管理服务
/// </summary>
public static class StartupService
{
    private const string RegistryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "ClipPanda";

    /// <summary>
    /// 设置开机自启动状态
    /// </summary>
    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
            if (key == null) return;

            if (enable)
            {
                var exePath = GetExecutablePath();
                key.SetValue(AppName, exePath);
                LogService.Info($"开机自启动已启用: {exePath}");
            }
            else
            {
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                    LogService.Info("开机自启动已禁用");
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "设置开机自启动失败");
            throw;
        }
    }

    /// <summary>
    /// 检查当前开机自启动状态
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key == null) return false;

            var value = key.GetValue(AppName);
            if (value == null) return false;

            // 检查路径是否有效
            var currentPath = GetExecutablePath();
            return string.Equals(value.ToString(), currentPath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取当前可执行文件路径
    /// </summary>
    private static string GetExecutablePath()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().Location;
    }
}
