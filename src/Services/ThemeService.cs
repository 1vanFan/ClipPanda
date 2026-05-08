using Application = System.Windows.Application;

namespace ClipPanda.Services;

/// <summary>
/// 主题配色定义
/// </summary>
public static class ThemeColors
{
    public static readonly Dictionary<string, ThemeDefinition> LightThemes = new()
    {
        ["Blue"] = new ThemeDefinition
        {
            Primary = "#2B579A",
            Accent = "#0078D4",
            Light = "#E8F4FC",
            Background = "#F3F3F3",
            CardBackground = "#FFFFFF",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#666666",
            TextMuted = "#999999",
            Border = "#E0E0E0"
        },
        ["Green"] = new ThemeDefinition
        {
            Primary = "#107C10",
            Accent = "#16C60C",
            Light = "#E8F5E9",
            Background = "#F3F3F3",
            CardBackground = "#FFFFFF",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#666666",
            TextMuted = "#999999",
            Border = "#E0E0E0"
        },
        ["Purple"] = new ThemeDefinition
        {
            Primary = "#5C2D91",
            Accent = "#8764B8",
            Light = "#F3E8FD",
            Background = "#F3F3F3",
            CardBackground = "#FFFFFF",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#666666",
            TextMuted = "#999999",
            Border = "#E0E0E0"
        },
        ["Orange"] = new ThemeDefinition
        {
            Primary = "#D83B01",
            Accent = "#FF8C00",
            Light = "#FFF3E0",
            Background = "#F3F3F3",
            CardBackground = "#FFFFFF",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#666666",
            TextMuted = "#999999",
            Border = "#E0E0E0"
        },
        ["Pink"] = new ThemeDefinition
        {
            Primary = "#C30052",
            Accent = "#E91E63",
            Light = "#FCE4EC",
            Background = "#F3F3F3",
            CardBackground = "#FFFFFF",
            TextPrimary = "#1A1A1A",
            TextSecondary = "#666666",
            TextMuted = "#999999",
            Border = "#E0E0E0"
        }
    };

    public static readonly Dictionary<string, ThemeDefinition> DarkThemes = new()
    {
        ["Blue"] = new ThemeDefinition
        {
            Primary = "#4A9EFF",
            Accent = "#0078D4",
            Light = "#1E3A5F",
            Background = "#1E1E1E",
            CardBackground = "#2D2D2D",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            TextMuted = "#888888",
            Border = "#404040"
        },
        ["Green"] = new ThemeDefinition
        {
            Primary = "#4CAF50",
            Accent = "#16C60C",
            Light = "#1E3A1E",
            Background = "#1E1E1E",
            CardBackground = "#2D2D2D",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            TextMuted = "#888888",
            Border = "#404040"
        },
        ["Purple"] = new ThemeDefinition
        {
            Primary = "#9C27B0",
            Accent = "#8764B8",
            Light = "#3A1E5F",
            Background = "#1E1E1E",
            CardBackground = "#2D2D2D",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            TextMuted = "#888888",
            Border = "#404040"
        },
        ["Orange"] = new ThemeDefinition
        {
            Primary = "#FF9800",
            Accent = "#FF8C00",
            Light = "#5F3A1E",
            Background = "#1E1E1E",
            CardBackground = "#2D2D2D",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            TextMuted = "#888888",
            Border = "#404040"
        },
        ["Pink"] = new ThemeDefinition
        {
            Primary = "#E91E63",
            Accent = "#F06292",
            Light = "#5F1E3A",
            Background = "#1E1E1E",
            CardBackground = "#2D2D2D",
            TextPrimary = "#FFFFFF",
            TextSecondary = "#CCCCCC",
            TextMuted = "#888888",
            Border = "#404040"
        }
    };
}

/// <summary>
/// 主题定义
/// </summary>
public class ThemeDefinition
{
    public string Primary { get; set; } = "#2B579A";
    public string Accent { get; set; } = "#0078D4";
    public string Light { get; set; } = "#E8F4FC";
    public string Background { get; set; } = "#F3F3F3";
    public string CardBackground { get; set; } = "#FFFFFF";
    public string TextPrimary { get; set; } = "#1A1A1A";
    public string TextSecondary { get; set; } = "#666666";
    public string TextMuted { get; set; } = "#999999";
    public string Border { get; set; } = "#E0E0E0";
}

/// <summary>
/// 主题服务
/// </summary>
public static class ThemeService
{
    private static bool _isDarkMode = false;

    /// <summary>
    /// 当前是否为深色模式
    /// </summary>
    public static bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// 应用主题
    /// </summary>
    public static void ApplyTheme(string themeName, bool? useDarkMode = null)
    {
        // 确定是否使用深色模式
        if (useDarkMode.HasValue)
        {
            _isDarkMode = useDarkMode.Value;
        }
        else
        {
            // 跟随系统主题
            _isDarkMode = IsSystemDarkMode();
        }

        var themes = _isDarkMode ? ThemeColors.DarkThemes : ThemeColors.LightThemes;

        if (!themes.TryGetValue(themeName, out var theme))
            theme = themes["Blue"];

        var resources = Application.Current.Resources;

        var primaryColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Primary);
        var accentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Accent);
        var lightColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Light);
        var backgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Background);
        var cardBackgroundColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.CardBackground);
        var textPrimaryColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.TextPrimary);
        var textSecondaryColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.TextSecondary);
        var textMutedColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.TextMuted);
        var borderColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.Border);

        // 更新颜色资源
        resources["PrimaryColor"] = primaryColor;
        resources["AccentColor"] = accentColor;
        resources["BackgroundColor"] = backgroundColor;
        resources["CardBackgroundColor"] = cardBackgroundColor;
        resources["TextPrimaryColor"] = textPrimaryColor;
        resources["TextSecondaryColor"] = textSecondaryColor;
        resources["TextMutedColor"] = textMutedColor;
        resources["BorderColor"] = borderColor;

        // 更新画刷
        resources["PrimaryBrush"] = new System.Windows.Media.SolidColorBrush(primaryColor);
        resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(accentColor);
        resources["BackgroundBrush"] = new System.Windows.Media.SolidColorBrush(backgroundColor);
        resources["CardBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(cardBackgroundColor);
        resources["TextPrimaryBrush"] = new System.Windows.Media.SolidColorBrush(textPrimaryColor);
        resources["TextSecondaryBrush"] = new System.Windows.Media.SolidColorBrush(textSecondaryColor);
        resources["TextMutedBrush"] = new System.Windows.Media.SolidColorBrush(textMutedColor);
        resources["BorderBrush"] = new System.Windows.Media.SolidColorBrush(borderColor);
        resources["SelectedItemBackground"] = new System.Windows.Media.SolidColorBrush(lightColor);

        LogService.Info($"主题应用: {themeName}, 深色模式: {_isDarkMode}");
    }

    /// <summary>
    /// 切换深色/浅色模式
    /// </summary>
    public static void ToggleDarkMode(string themeName)
    {
        ApplyTheme(themeName, !_isDarkMode);
    }

    /// <summary>
    /// 检测系统是否为深色模式
    /// </summary>
    private static bool IsSystemDarkMode()
    {
        try
        {
            // 读取注册表检测系统主题
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = 深色模式
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error(ex, "检测系统主题失败");
        }
        return false; // 默认浅色
    }

    /// <summary>
    /// 获取主题主色
    /// </summary>
    public static string GetPrimaryColor(string themeName, bool isDarkMode)
    {
        var themes = isDarkMode ? ThemeColors.DarkThemes : ThemeColors.LightThemes;
        return themes.TryGetValue(themeName, out var theme)
            ? theme.Primary
            : themes["Blue"].Primary;
    }
}
