using System.Globalization;
using System.Resources;
using System.Threading;

namespace ClipPanda.Services;

/// <summary>
/// 本地化服务
/// </summary>
public static class LocalizationService
{
    private static readonly ResourceManager ResourceManager = new(
        "ClipPanda.Resources.Strings",
        typeof(LocalizationService).Assembly);

    private static CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

    /// <summary>
    /// 当前语言文化
    /// </summary>
    public static CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            _currentCulture = value;
            Thread.CurrentThread.CurrentUICulture = value;
            Thread.CurrentThread.CurrentCulture = value;
            CultureInfo.CurrentUICulture = value;
            CultureInfo.CurrentCulture = value;
        }
    }

    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _currentCulture) ?? key;
    }

    /// <summary>
    /// 获取格式化后的本地化字符串
    /// </summary>
    public static string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(_currentCulture, format, args);
    }

    /// <summary>
    /// 切换到指定语言
    /// </summary>
    public static void SetLanguage(string languageCode)
    {
        CurrentCulture = new CultureInfo(languageCode);
        LogService.Info($"语言切换为: {languageCode}");
    }

    /// <summary>
    /// 支持的语言列表
    /// </summary>
    public static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        ["zh-CN"] = "简体中文",
        ["en"] = "English"
    };
}

/// <summary>
/// 本地化字符串扩展方法
/// </summary>
public static class LocalizationExtensions
{
    public static string _(this string key) => LocalizationService.GetString(key);
    public static string _(this string key, params object[] args) => LocalizationService.GetString(key, args);
}
