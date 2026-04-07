using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodePrint.Models;

namespace CodePrint.Services;

/// <summary>
/// Handles saving and loading label documents in the .btq (番茄标签) format.
/// The .btq format is JSON-based for readability and compatibility.
/// </summary>
public static class FileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new LabelElementConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public const string FileExtension = ".btq";
    public const string FileFilter = "番茄标签文件 (*.btq)|*.btq|所有文件 (*.*)|*.*";

    /// <summary>
    /// Saves a label document to a .btq file.
    /// </summary>
    public static void Save(LabelDocument document, string filePath)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a label document from a .btq file.
    /// </summary>
    public static LabelDocument Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<LabelDocument>(json, JsonOptions)
               ?? throw new InvalidDataException("无法读取标签文件");
    }
}

/// <summary>
/// Custom JSON converter for polymorphic LabelElement serialization/deserialization.
/// </summary>
public class LabelElementConverter : JsonConverter<LabelElement>
{
    public override LabelElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeProp))
            return null;

        var elementType = typeProp.Deserialize<ElementType>(options);
        var innerOptions = CreateInnerOptions(options);
        var rawJson = root.GetRawText();

        return elementType switch
        {
            ElementType.Text => JsonSerializer.Deserialize<TextElement>(rawJson, innerOptions),
            ElementType.Barcode => JsonSerializer.Deserialize<BarcodeElement>(rawJson, innerOptions),
            ElementType.QrCode => JsonSerializer.Deserialize<QrCodeElement>(rawJson, innerOptions),
            ElementType.LinkedQrCode => JsonSerializer.Deserialize<LinkedQrCodeElement>(rawJson, innerOptions),
            ElementType.Image => JsonSerializer.Deserialize<ImageElement>(rawJson, innerOptions),
            ElementType.Icon => JsonSerializer.Deserialize<IconElement>(rawJson, innerOptions),
            ElementType.Line => JsonSerializer.Deserialize<LineElement>(rawJson, innerOptions),
            ElementType.Rectangle => JsonSerializer.Deserialize<RectangleElement>(rawJson, innerOptions),
            ElementType.Date => JsonSerializer.Deserialize<DateElement>(rawJson, innerOptions),
            ElementType.Table => JsonSerializer.Deserialize<TableElement>(rawJson, innerOptions),
            ElementType.Pdf => JsonSerializer.Deserialize<PdfElement>(rawJson, innerOptions),
            ElementType.Warning => JsonSerializer.Deserialize<WarningElement>(rawJson, innerOptions),
            ElementType.Watermark => JsonSerializer.Deserialize<WatermarkElement>(rawJson, innerOptions),
            _ => JsonSerializer.Deserialize<LabelElement>(rawJson, innerOptions)
        };
    }

    public override void Write(Utf8JsonWriter writer, LabelElement value, JsonSerializerOptions options)
    {
        var innerOptions = CreateInnerOptions(options);
        JsonSerializer.Serialize(writer, value, value.GetType(), innerOptions);
    }

    /// <summary>
    /// Creates a copy of the options without this converter to avoid infinite recursion.
    /// </summary>
    private static JsonSerializerOptions CreateInnerOptions(JsonSerializerOptions options)
    {
        var innerOptions = new JsonSerializerOptions(options);
        for (int i = innerOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (innerOptions.Converters[i] is LabelElementConverter)
            {
                innerOptions.Converters.RemoveAt(i);
                break;
            }
        }
        return innerOptions;
    }
}
