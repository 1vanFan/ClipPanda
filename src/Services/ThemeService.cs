using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ClipPanda.Services;

/// <summary>
/// 主题配色定义
/// </summary>
public static class ThemeColors
{
    public static readonly Dictionary<string, ThemeDefinition> Themes = new()
    {
        ["Blue"] = new ThemeDefinition
        {
            Primary = "#2B579A",
            Accent = "#0078D4",
            Light = "#E8F4FC"
        },
        ["Green"] = new ThemeDefinition
        {
            Primary = "#107C10",
            Accent = "#16C60C",
            Light = "#E8F5E9"
        },
        ["Purple"] = new ThemeDefinition
        {
            Primary = "#5C2D91",
            Accent = "#8764B8",
            Light = "#F3E8FD"
        },
        ["Orange"] = new ThemeDefinition
        {
            Primary = "#D83B01",
            Accent = "#FF8C00",
            Light = "#FFF3E0"
        },
        ["Pink"] = new ThemeDefinition
        {
            Primary = "#C30052",
            Accent = "#E91E63",
            Light = "#FCE4EC"
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
}

/// <summary>
/// 主题服务
/// </summary>
public static class ThemeService
{
    /// <summary>
    /// 应用主题
    /// </summary>
    public static void ApplyTheme(string themeName)
    {
        if (!ThemeColors.Themes.TryGetValue(themeName, out var theme))
            theme = ThemeColors.Themes["Blue"];

        var resources = Application.Current.Resources;

        // 更新颜色资源
        resources["PrimaryColor"] = (Color)ColorConverter.ConvertFromString(theme.Primary);
        resources["AccentColor"] = (Color)ColorConverter.ConvertFromString(theme.Accent);

        // 更新画刷
        resources["PrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Primary));
        resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Accent));

        // 更新选中项背景色
        resources["SelectedItemBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(theme.Light));
    }

    /// <summary>
    /// 获取主题主色
    /// </summary>
    public static string GetPrimaryColor(string themeName)
    {
        return ThemeColors.Themes.TryGetValue(themeName, out var theme)
            ? theme.Primary
            : ThemeColors.Themes["Blue"].Primary;
    }
}
