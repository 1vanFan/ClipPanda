using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;
using ClipPanda.Models;
using ClipPanda.Services;

namespace ClipPanda.ViewModels;

/// <summary>
/// 剪贴板条目视图模型
/// </summary>
public class ClipboardItemViewModel : INotifyPropertyChanged
{
    private readonly ClipboardItem _item;

    public ClipboardItemViewModel(ClipboardItem item)
    {
        _item = item;
    }

    public int Id => _item.Id;
    public ClipboardItem Item => _item;

    public ContentType ContentType => _item.ContentType;

    public string ContentTypeIcon => _item.ContentType switch
    {
        ContentType.Text => "T",
        ContentType.Html => "H",
        ContentType.Image => "IMG",
        ContentType.Files => "F",
        ContentType.Rtf => "R",
        _ => "?"
    };

    public string ContentTypeColor => _item.ContentType switch
    {
        ContentType.Text => "#2B579A",
        ContentType.Html => "#0078D4",
        ContentType.Image => "#107C10",
        ContentType.Files => "#D83B01",
        ContentType.Rtf => "#8764B8",
        _ => "#666666"
    };

    public string Preview => _item.GetDisplayPreview(80);

    public string TimeDisplay
    {
        get
        {
            var diff = DateTime.Now - _item.CopyTime;

            if (diff.TotalMinutes < 1)
                return "刚刚";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} 分钟前";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} 小时前";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} 天前";

            return _item.CopyTime.ToString("MM-dd HH:mm");
        }
    }

    public bool IsFavorited => _item.IsFavorited;

    public Visibility FavoriteVisibility => _item.IsFavorited
        ? Visibility.Visible
        : Visibility.Collapsed;

    /// <summary>
    /// 是否为图片类型
    /// </summary>
    public bool IsImage => _item.ContentType == ContentType.Image;

    /// <summary>
    /// 图片缩略图（如果是图片类型）
    /// </summary>
    public BitmapImage? ThumbnailImage
    {
        get
        {
            if (_item.ContentType != ContentType.Image || _item.BinaryContent == null)
                return null;

            try
            {
                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(_item.BinaryContent))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 64; // 限制解码尺寸
                    bitmap.EndInit();
                }
                bitmap.Freeze(); // 冻结以提高性能
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 主窗口视图模型
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly DatabaseService _databaseService;
    private ObservableCollection<ClipboardItemViewModel> _clipboardItems = new();
    private ClipboardItemViewModel? _selectedItem;
    private string _searchText = string.Empty;
    private string _statusText = "就绪";
    private bool _showFavoritesOnly;

    public MainViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        _ = LoadItemsAsync();
    }

    public ObservableCollection<ClipboardItemViewModel> ClipboardItems
    {
        get => _clipboardItems;
        set
        {
            _clipboardItems = value;
            OnPropertyChanged();
        }
    }

    public ClipboardItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            _selectedItem = value;
            OnPropertyChanged();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            _searchText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        set
        {
            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// 是否仅显示收藏
    /// </summary>
    public bool ShowFavoritesOnly
    {
        get => _showFavoritesOnly;
        set
        {
            _showFavoritesOnly = value;
            OnPropertyChanged();
            _ = LoadItemsAsync();
        }
    }

    public async Task LoadItemsAsync()
    {
        List<ClipboardItem> items;

        if (_showFavoritesOnly)
        {
            // 仅显示收藏
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                items = await _databaseService.GetFavoriteItemsAsync(100);
            }
            else
            {
                items = await _databaseService.SearchFavoriteItemsAsync(SearchText, 100);
            }
        }
        else
        {
            // 显示全部
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                items = await _databaseService.GetAllItemsAsync(100);
            }
            else
            {
                items = await _databaseService.SearchItemsAsync(SearchText, 100);
            }
        }

        ClipboardItems = new ObservableCollection<ClipboardItemViewModel>(
            items.Select(i => new ClipboardItemViewModel(i)));

        var tabLabel = _showFavoritesOnly ? "收藏" : "全部";
        StatusText = $"{tabLabel} - 共 {ClipboardItems.Count} 条记录";

        // 更新空状态显示
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
    }

    /// <summary>
    /// 是否为空状态
    /// </summary>
    public bool IsEmpty => ClipboardItems.Count == 0;

    /// <summary>
    /// 空状态标题
    /// </summary>
    public string EmptyStateTitle => _showFavoritesOnly ? "暂无收藏" : "暂无历史记录";

    /// <summary>
    /// 空状态描述
    /// </summary>
    public string EmptyStateDescription
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
                return "未找到匹配的内容，请尝试其他关键词";
            return _showFavoritesOnly
                ? "点击收藏按钮将常用内容添加到此处"
                : "复制任意内容后将自动出现在这里";
        }
    }

    public void RefreshItems()
    {
        _ = LoadItemsAsync();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
