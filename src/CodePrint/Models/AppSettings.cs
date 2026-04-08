namespace CodePrint.Models;

/// <summary>应用语言。</summary>
public enum AppLanguage { Chinese, English }

/// <summary>
/// 应用程序全局设置，持久化到用户本地 AppData。
/// </summary>
public class AppSettings
{
    // ── 语言 ──
    public AppLanguage Language { get; set; } = AppLanguage.Chinese;

    // ── 打印 ──
    /// <summary>打印 DPI（清晰度），常见值：150、203、300、600。</summary>
    public int PrintDpi { get; set; } = 600;

    // ── 默认标签尺寸 ──
    public double DefaultPaperWidthMm { get; set; } = 50;
    public double DefaultPaperHeightMm { get; set; } = 30;
    public PrintOrientation DefaultOrientation { get; set; } = PrintOrientation.Portrait;

    // ── 自动保存 ──
    public bool AutoSaveEnabled { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 60;

    // ── 网格与对齐 ──
    public bool ShowGrid { get; set; }
    public double GridSpacingMm { get; set; } = 5.0;
    public bool SnapToGrid { get; set; } = true;

    // ── 默认背景色 ──
    public string DefaultBackgroundColor { get; set; } = "#FFFFFF";

    // ── 移动步进 ──
    /// <summary>方向键移动距离 (mm)。</summary>
    public double NudgeDistanceMm { get; set; } = 0.265;
    /// <summary>Shift+方向键移动距离 (mm)。</summary>
    public double LargeNudgeDistanceMm { get; set; } = 2.65;
}
