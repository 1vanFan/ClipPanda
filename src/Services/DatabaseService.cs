using System.IO;
using Microsoft.EntityFrameworkCore;
using ClipPanda.Models;

namespace ClipPanda.Services;

/// <summary>
/// 数据库上下文
/// </summary>
public class ClipboardDbContext : DbContext
{
    public DbSet<ClipboardItem> ClipboardItems { get; set; } = null!;

    private readonly string _dbPath;

    public ClipboardDbContext(string? dbPath = null)
    {
        _dbPath = dbPath ?? GetDefaultDbPath();
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClipboardItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ContentType).HasConversion<int>();
            entity.Property(e => e.Preview).HasMaxLength(500);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.HasIndex(e => e.CopyTime);
            entity.HasIndex(e => e.ContentHash);
            entity.HasIndex(e => e.IsFavorited);
        });
    }

    private static string GetDefaultDbPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "ClipPanda", "clipboard.db");
    }
}

/// <summary>
/// 数据库服务
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly ClipboardDbContext _context;

    public DatabaseService(string? dbPath = null)
    {
        _context = new ClipboardDbContext(dbPath);
        _context.Database.EnsureCreated();
    }

    /// <summary>
    /// 添加剪贴板条目
    /// </summary>
    public async Task<int> AddItemAsync(ClipboardItem item)
    {
        _context.ClipboardItems.Add(item);
        await _context.SaveChangesAsync();
        return item.Id;
    }

    /// <summary>
    /// 获取所有条目（按时间倒序）
    /// </summary>
    public async Task<List<ClipboardItem>> GetAllItemsAsync(int limit = 100)
    {
        return await _context.ClipboardItems
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 搜索条目
    /// </summary>
    public async Task<List<ClipboardItem>> SearchItemsAsync(string keyword, int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllItemsAsync(limit);

        var lowerKeyword = keyword.ToLower();
        return await _context.ClipboardItems
            .Where(i => i.Preview != null && i.Preview.ToLower().Contains(lowerKeyword))
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 获取收藏条目
    /// </summary>
    public async Task<List<ClipboardItem>> GetFavoriteItemsAsync(int limit = 100)
    {
        return await _context.ClipboardItems
            .Where(i => i.IsFavorited)
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 按内容类型获取条目
    /// </summary>
    public async Task<List<ClipboardItem>> GetItemsByTypeAsync(ContentType? contentType, int limit = 100)
    {
        var query = _context.ClipboardItems.AsQueryable();
        
        if (contentType.HasValue)
        {
            query = query.Where(i => i.ContentType == contentType.Value);
        }
        
        return await query
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 按内容类型搜索条目
    /// </summary>
    public async Task<List<ClipboardItem>> SearchItemsByTypeAsync(string keyword, ContentType? contentType, int limit = 100)
    {
        var query = _context.ClipboardItems.AsQueryable();
        
        if (contentType.HasValue)
        {
            query = query.Where(i => i.ContentType == contentType.Value);
        }
        
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            query = query.Where(i => i.Preview != null && i.Preview.ToLower().Contains(lowerKeyword));
        }
        
        return await query
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 按内容类型获取收藏条目
    /// </summary>
    public async Task<List<ClipboardItem>> GetFavoriteItemsByTypeAsync(ContentType? contentType, int limit = 100)
    {
        var query = _context.ClipboardItems.Where(i => i.IsFavorited);
        
        if (contentType.HasValue)
        {
            query = query.Where(i => i.ContentType == contentType.Value);
        }
        
        return await query
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 搜索收藏条目
    /// </summary>
    public async Task<List<ClipboardItem>> SearchFavoriteItemsAsync(string keyword, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetFavoriteItemsAsync(limit);

        var lowerKeyword = keyword.ToLower();
        return await _context.ClipboardItems
            .Where(i => i.IsFavorited && i.Preview != null && i.Preview.ToLower().Contains(lowerKeyword))
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 根据ID获取条目
    /// </summary>
    public async Task<ClipboardItem?> GetItemByIdAsync(int id)
    {
        return await _context.ClipboardItems.FindAsync(id);
    }

    /// <summary>
    /// 更新条目
    /// </summary>
    public async Task UpdateItemAsync(ClipboardItem item)
    {
        _context.ClipboardItems.Update(item);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 删除条目
    /// </summary>
    public async Task DeleteItemAsync(int id)
    {
        var item = await _context.ClipboardItems.FindAsync(id);
        if (item != null)
        {
            _context.ClipboardItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 根据内容哈希查找条目
    /// </summary>
    public async Task<ClipboardItem?> FindByHashAsync(string hash)
    {
        return await _context.ClipboardItems
            .FirstOrDefaultAsync(i => i.ContentHash == hash);
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    public async Task IncrementUseCountAsync(int id)
    {
        var item = await _context.ClipboardItems.FindAsync(id);
        if (item != null)
        {
            item.UseCount++;
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 清理过期条目
    /// </summary>
    public async Task<int> CleanupExpiredItemsAsync(int retentionDays)
    {
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        var expiredItems = await _context.ClipboardItems
            .Where(i => !i.IsFavorited && i.CopyTime < cutoffDate)
            .ToListAsync();

        _context.ClipboardItems.RemoveRange(expiredItems);
        await _context.SaveChangesAsync();

        return expiredItems.Count;
    }

    /// <summary>
    /// 获取条目总数
    /// </summary>
    public async Task<int> GetItemCountAsync()
    {
        return await _context.ClipboardItems.CountAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
