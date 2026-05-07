using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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
        ContentType.Text => "📝",
        ContentType.Html => "🌐",
        ContentType.Image => "🖼️",
        ContentType.Files => "📁",
        ContentType.Rtf => "📄",
        _ => "📋"
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

    public async Task LoadItemsAsync()
    {
        var items = string.IsNullOrWhiteSpace(SearchText)
            ? await _databaseService.GetAllItemsAsync(100)
            : await _databaseService.SearchItemsAsync(SearchText, 100);

        ClipboardItems = new ObservableCollection<ClipboardItemViewModel>(
            items.Select(i => new ClipboardItemViewModel(i)));

        StatusText = $"共 {ClipboardItems.Count} 条记录";
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
