using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// 管理标签文档的本地持久化存储。
/// 所有标签保存在 %LocalAppData%\CodePrint\Labels\ 目录下，以 .btq 格式存储。
/// </summary>
public static class LabelStorageService
{
    private static readonly string StorageDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodePrint", "Labels");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new LabelElementConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>确保存储目录存在。</summary>
    private static void EnsureDirectory()
    {
        Directory.CreateDirectory(StorageDir);
    }

    /// <summary>获取标签文件路径。</summary>
    private static string GetFilePath(string documentId)
    {
        return Path.Combine(StorageDir, documentId + FileService.FileExtension);
    }

    /// <summary>保存标签文档。</summary>
    public static void Save(LabelDocument document)
    {
        EnsureDirectory();
        document.ModifiedAt = DateTime.Now;
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(GetFilePath(document.Id), json);
    }

    /// <summary>加载所有已保存的标签文档。</summary>
    public static List<LabelDocument> LoadAll()
    {
        EnsureDirectory();
        var results = new List<LabelDocument>();
        foreach (var file in Directory.GetFiles(StorageDir, "*" + FileService.FileExtension))
        {
            try
            {
                var json = File.ReadAllText(file);
                var doc = JsonSerializer.Deserialize<LabelDocument>(json, JsonOptions);
                if (doc != null)
                    results.Add(doc);
            }
            catch
            {
                // 跳过损坏的文件
            }
        }
        return results.OrderByDescending(d => d.ModifiedAt).ToList();
    }

    /// <summary>加载单个标签文档。</summary>
    public static LabelDocument? Load(string documentId)
    {
        var path = GetFilePath(documentId);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LabelDocument>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>删除标签文档。</summary>
    public static bool Delete(string documentId)
    {
        var path = GetFilePath(documentId);
        if (File.Exists(path))
        {
            File.Delete(path);
            return true;
        }
        return false;
    }

    /// <summary>检查标签是否已存储。</summary>
    public static bool Exists(string documentId)
    {
        return File.Exists(GetFilePath(documentId));
    }
}
