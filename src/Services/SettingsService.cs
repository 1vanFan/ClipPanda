using System.IO;
using System.Text.Json;
using ClipPanda.Models;

namespace ClipPanda.Services;

/// <summary>
/// 设置持久化服务 - 使用 JSON 文件存储
/// </summary>
public class SettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClipPanda", "settings.json");

    private AppSettings _settings;

    public SettingsService()
    {
        _settings = Load();
    }

    /// <summary>
    /// 当前设置
    /// </summary>
    public AppSettings Settings => _settings;

    /// <summary>
    /// 从文件加载设置
    /// </summary>
    private static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClipPanda] 加载设置失败: {ex.Message}");
        }

        return new AppSettings();
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath)!;
            Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClipPanda] 保存设置失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新设置并保存
    /// </summary>
    public void Update(Action<AppSettings> updateAction)
    {
        updateAction(_settings);
        Save();
    }
}
