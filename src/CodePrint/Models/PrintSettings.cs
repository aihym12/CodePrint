namespace CodePrint.Models;

public enum ColorMode { Color, BlackAndWhite, Grayscale }
public enum PrintRange { All, CurrentPage, Custom }

public class PrintSettings
{
    public string PrinterName { get; set; } = string.Empty;
    public double PaperWidth { get; set; } = 50;
    public double PaperHeight { get; set; } = 30;
    public PrintOrientation Orientation { get; set; } = PrintOrientation.Portrait;
    public int Copies { get; set; } = 1;
    public PrintRange Range { get; set; } = PrintRange.All;
    public string CustomPageRange { get; set; } = string.Empty;
    public ColorMode ColorMode { get; set; } = ColorMode.Color;
    public int LabelsPerRow { get; set; } = 1;
    public int LabelsPerColumn { get; set; } = 1;
    public bool EnableRegistrationPrint { get; set; }
    public bool ShowCutLines { get; set; }
    public bool MirrorPrint { get; set; }

    /// <summary>打印 DPI（清晰度），0 表示使用全局默认值。常见值：150、203、300、600。</summary>
    public int PrintDpi { get; set; }
}
