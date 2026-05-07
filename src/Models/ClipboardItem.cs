namespace ClipPanda.Models;

/// <summary>
/// 剪贴板内容类型
/// </summary>
public enum ContentType
{
    Text = 0,
    Html = 1,
    Image = 2,
    Files = 3,
    Rtf = 4,
    Other = 99
}

/// <summary>
/// 剪贴板条目实体
/// </summary>
public class ClipboardItem
{
    public int Id { get; set; }

    /// <summary>
    /// 内容类型
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    /// 文本内容（用于文本、HTML、RTF等）
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// 二进制内容（用于图片等）
    /// </summary>
    public byte[]? BinaryContent { get; set; }

    /// <summary>
    /// 预览文本（列表显示用）
    /// </summary>
    public string Preview { get; set; } = string.Empty;

    /// <summary>
    /// 复制时间
    /// </summary>
    public DateTime CopyTime { get; set; }

    /// <summary>
    /// 使用次数（粘贴次数）
    /// </summary>
    public int UseCount { get; set; }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    public bool IsFavorited { get; set; }

    /// <summary>
    /// 来源应用名称
    /// </summary>
    public string? SourceApp { get; set; }

    /// <summary>
    /// 内容哈希（用于去重）
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// 获取显示用的预览文本
    /// </summary>
    public string GetDisplayPreview(int maxLength = 100)
    {
        if (string.IsNullOrEmpty(Preview))
            return ContentType switch
            {
                ContentType.Image => "[图片]",
                ContentType.Files => "[文件]",
                ContentType.Html => "[HTML]",
                ContentType.Rtf => "[富文本]",
                _ => "[空内容]"
            };

        return Preview.Length > maxLength
            ? Preview[..maxLength] + "..."
            : Preview;
    }
}
