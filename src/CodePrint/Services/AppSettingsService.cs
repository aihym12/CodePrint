using System.IO;
using System.Text.Json;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// 全局应用设置的持久化服务（单例）。
/// 设置保存为 JSON 文件，位于 %LocalAppData%/CodePrint/app_settings.json。
/// </summary>
public static class AppSettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodePrint");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "app_settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static AppSettings? _cached;

    /// <summary>获取当前设置（首次调用时从磁盘加载）。</summary>
    public static AppSettings Current => _cached ??= Load();

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings != null)
                {
                    _cached = settings;
                    return settings;
                }
            }
        }
        catch
        {
            // 文件损坏——返回默认值
        }

        var defaults = new AppSettings();
        _cached = defaults;
        return defaults;
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            _cached = settings;
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // 忽略写入失败（权限、磁盘满等）
        }
    }

    /// <summary>重置为默认值并保存。</summary>
    public static AppSettings Reset()
    {
        var defaults = new AppSettings();
        Save(defaults);
        return defaults;
    }
}
