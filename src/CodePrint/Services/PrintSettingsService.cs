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
        catch
        {
            // 文件损坏——返回默认值
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
        catch
        {
            // 忽略写入失败（权限、磁盘满等）
        }
    }
}
