namespace ClipPanda.Models;

/// <summary>
/// 应用配置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 历史记录最大条数
    /// </summary>
    public int MaxHistoryCount { get; set; } = 500;

    /// <summary>
    /// 历史记录保留天数
    /// </summary>
    public int HistoryRetentionDays { get; set; } = 7;

    /// <summary>
    /// 是否开机自启
    /// </summary>
    public bool StartWithWindows { get; set; } = true;

    /// <summary>
    /// 主快捷键
    /// </summary>
    public string MainHotkey { get; set; } = "Ctrl+Shift+C";

    /// <summary>
    /// 是否启用自动去重
    /// </summary>
    public bool EnableAutoDeduplication { get; set; } = true;

    /// <summary>
    /// 是否启用敏感内容检测
    /// </summary>
    public bool EnableSensitiveContentDetection { get; set; } = true;

    /// <summary>
    /// 是否启用深色模式（null表示跟随系统）
    /// </summary>
    public bool? UseDarkMode { get; set; } = null;

    /// <summary>
    /// 面板宽度
    /// </summary>
    public int PanelWidth { get; set; } = 600;

    /// <summary>
    /// 面板高度
    /// </summary>
    public int PanelHeight { get; set; } = 500;

    /// <summary>
    /// 是否最小化到托盘
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// 是否显示复制通知
    /// </summary>
    public bool ShowCopyNotification { get; set; } = false;

    /// <summary>
    /// 最后选中的历史记录ID
    /// </summary>
    public int LastSelectedItemId { get; set; } = 0;

    /// <summary>
    /// 最后选中的Tab（0=全部，1=收藏）
    /// </summary>
    public int LastSelectedTab { get; set; } = 0;

    /// <summary>
    /// 主题配色（Blue, Green, Purple, Orange, Pink）
    /// </summary>
    public string ThemeColor { get; set; } = "Blue";
}
