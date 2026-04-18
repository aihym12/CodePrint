using System.Diagnostics;
using System.IO;
using System.Text.Json;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// 打印设置的持久化服务。
/// 设置保存为 JSON 文件，位于 %LocalAppData%/CodePrint/print_settings.json。
/// </summary>
public static class PrintSettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodePrint");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "print_settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static PrintSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<PrintSettings>(json, JsonOptions) ?? new PrintSettings();
            }
        }
        catch (Exception ex)
        {
            // 文件损坏——返回默认值，但要记录原因便于排查。
            Debug.WriteLine($"[PrintSettings] 读取失败，使用默认值: {ex.Message}");
        }
        return new PrintSettings();
    }

    public static void Save(PrintSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFile, json);
        }
        catch (Exception ex)
        {
            // 写入失败（权限不足、磁盘满等）——记录日志，避免静默丢失用户配置。
            Debug.WriteLine($"[PrintSettings] 保存失败: {ex.Message}");
        }
    }
}
