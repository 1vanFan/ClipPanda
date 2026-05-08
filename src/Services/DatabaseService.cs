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

    public static string GetDefaultDbPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "ClipPanda", "clipboard.db");
    }
}

/// <summary>
/// 数据库服务 - 使用 DbContext 工厂模式支持多线程访问
/// </summary>
public class DatabaseService
{
    private readonly string _dbPath;

    public DatabaseService(string? dbPath = null)
    {
        _dbPath = dbPath ?? ClipboardDbContext.GetDefaultDbPath();
        // 确保数据库创建
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>
    /// 创建新的 DbContext 实例
    /// </summary>
    private ClipboardDbContext CreateContext()
    {
        return new ClipboardDbContext(_dbPath);
    }

    /// <summary>
    /// 添加剪贴板条目
    /// </summary>
    public async Task<int> AddItemAsync(ClipboardItem item)
    {
        using var context = CreateContext();
        context.ClipboardItems.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    /// <summary>
    /// 获取所有条目（按时间倒序）
    /// </summary>
    public async Task<List<ClipboardItem>> GetAllItemsAsync(int limit = 100)
    {
        using var context = CreateContext();
        return await context.ClipboardItems
            .OrderByDescending(i => i.CopyTime)
            .Take(limit)
            .ToListAsync();
    }

    /// <summary>
    /// 搜索条目
    /// </summary>
    public async Task<List<ClipboardItem>> SearchItemsAsync(string keyword, int limit = 50)
    {
        using var context = CreateContext();
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetAllItemsAsync(limit);

        var lowerKeyword = keyword.ToLower();
        return await context.ClipboardItems
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
        using var context = CreateContext();
        return await context.ClipboardItems
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
        using var context = CreateContext();
        var query = context.ClipboardItems.AsQueryable();
        
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
        using var context = CreateContext();
        var query = context.ClipboardItems.AsQueryable();
        
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
        using var context = CreateContext();
        var query = context.ClipboardItems.Where(i => i.IsFavorited);
        
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
        using var context = CreateContext();
        if (string.IsNullOrWhiteSpace(keyword))
            return await GetFavoriteItemsAsync(limit);

        var lowerKeyword = keyword.ToLower();
        return await context.ClipboardItems
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
        using var context = CreateContext();
        return await context.ClipboardItems.FindAsync(id);
    }

    /// <summary>
    /// 更新条目
    /// </summary>
    public async Task UpdateItemAsync(ClipboardItem item)
    {
        using var context = CreateContext();
        context.ClipboardItems.Update(item);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// 删除条目
    /// </summary>
    public async Task DeleteItemAsync(int id)
    {
        using var context = CreateContext();
        var item = await context.ClipboardItems.FindAsync(id);
        if (item != null)
        {
            context.ClipboardItems.Remove(item);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 根据内容哈希查找条目
    /// </summary>
    public async Task<ClipboardItem?> FindByHashAsync(string hash)
    {
        using var context = CreateContext();
        return await context.ClipboardItems
            .FirstOrDefaultAsync(i => i.ContentHash == hash);
    }

    /// <summary>
    /// 增加使用次数
    /// </summary>
    public async Task IncrementUseCountAsync(int id)
    {
        using var context = CreateContext();
        var item = await context.ClipboardItems.FindAsync(id);
        if (item != null)
        {
            item.UseCount++;
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 清理过期条目
    /// </summary>
    public async Task<int> CleanupExpiredItemsAsync(int retentionDays)
    {
        using var context = CreateContext();
        var cutoffDate = DateTime.Now.AddDays(-retentionDays);
        var expiredItems = await context.ClipboardItems
            .Where(i => !i.IsFavorited && i.CopyTime < cutoffDate)
            .ToListAsync();

        context.ClipboardItems.RemoveRange(expiredItems);
        await context.SaveChangesAsync();

        return expiredItems.Count;
    }

    /// <summary>
    /// 获取条目总数
    /// </summary>
    public async Task<int> GetItemCountAsync()
    {
        using var context = CreateContext();
        return await context.ClipboardItems.CountAsync();
    }
}
