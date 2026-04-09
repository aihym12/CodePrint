using System.IO;
using System.Text.Json;

namespace CodePrint.Services;

/// <summary>
/// Persists PDF crop &amp; print user preferences to a JSON file in AppData.
/// </summary>
public static class PdfCropSettingsService
{
    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodePrint");

    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "pdfcrop_settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static PdfCropSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<PdfCropSettings>(json, JsonOptions) ?? new PdfCropSettings();
            }
        }
        catch
        {
            // Corrupted file — return defaults
        }
        return new PdfCropSettings();
    }

    public static void Save(PdfCropSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Ignore write failures (permissions, disk full, etc.)
        }
    }
}

/// <summary>
/// Serializable settings for the PDF crop &amp; print module.
/// </summary>
public class PdfCropSettings
{
    // Paper size
    public int SelectedPaperSizeIndex { get; set; }
    public double PaperWidthMm { get; set; } = 100;
    public double PaperHeightMm { get; set; } = 100;
    public bool IsCustomSize { get; set; }
    public double CustomWidthMm { get; set; } = 80;
    public double CustomHeightMm { get; set; } = 60;

    // Print layout
    public int PrintLayoutIndex { get; set; }
    public bool ApplyCropToAllPages { get; set; } = true;

    // Density
    public string ImageDensity { get; set; } = "Auto";
    public int CustomDpi { get; set; } = 300;

    // Processing mode
    public string ProcessingMode { get; set; } = "PageCrop";

    // Last used printer
    public string? LastPrinterName { get; set; }

    /// <summary>打印边距（像素），上下左右各缩进该值。默认 5 像素。</summary>
    public double PrintMarginPx { get; set; } = 5;
}
