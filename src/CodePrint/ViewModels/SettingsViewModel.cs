using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;

namespace CodePrint.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // ── 语言 ──
    [ObservableProperty]
    private AppLanguage _language;

    /// <summary>语言选项列表（供 ComboBox 绑定）。</summary>
    public IReadOnlyList<AppLanguage> LanguageOptions { get; } =
        Enum.GetValues<AppLanguage>();

    // ── 打印 DPI ──
    [ObservableProperty]
    private int _printDpi;

    /// <summary>常见 DPI 选项。</summary>
    public IReadOnlyList<int> DpiOptions { get; } = PrintConstants.StandardDpiOptions;

    // ── 默认标签尺寸 ──
    [ObservableProperty]
    private double _defaultPaperWidthMm;

    [ObservableProperty]
    private double _defaultPaperHeightMm;

    [ObservableProperty]
    private PrintOrientation _defaultOrientation;

    // ── 自动保存 ──
    [ObservableProperty]
    private bool _autoSaveEnabled;

    [ObservableProperty]
    private int _autoSaveIntervalSeconds;

    // ── 网格与对齐 ──
    [ObservableProperty]
    private bool _showGrid;

    [ObservableProperty]
    private double _gridSpacingMm;

    [ObservableProperty]
    private bool _snapToGrid;

    // ── 默认背景色 ──
    [ObservableProperty]
    private string _defaultBackgroundColor = "#FFFFFF";

    // ── 移动步进 ──
    [ObservableProperty]
    private double _nudgeDistanceMm;

    [ObservableProperty]
    private double _largeNudgeDistanceMm;

    /// <summary>Raised when the dialog should close.</summary>
    public event Action<bool>? RequestClose;

    /// <summary>从持久化设置加载到 ViewModel。</summary>
    public void LoadFromSettings()
    {
        var s = AppSettingsService.Current;
        Language = s.Language;
        PrintDpi = s.PrintDpi;
        DefaultPaperWidthMm = s.DefaultPaperWidthMm;
        DefaultPaperHeightMm = s.DefaultPaperHeightMm;
        DefaultOrientation = s.DefaultOrientation;
        AutoSaveEnabled = s.AutoSaveEnabled;
        AutoSaveIntervalSeconds = s.AutoSaveIntervalSeconds;
        ShowGrid = s.ShowGrid;
        GridSpacingMm = s.GridSpacingMm;
        SnapToGrid = s.SnapToGrid;
        DefaultBackgroundColor = s.DefaultBackgroundColor;
        NudgeDistanceMm = s.NudgeDistanceMm;
        LargeNudgeDistanceMm = s.LargeNudgeDistanceMm;
    }

    [RelayCommand]
    private void Save()
    {
        var s = new AppSettings
        {
            Language = Language,
            PrintDpi = PrintDpi,
            DefaultPaperWidthMm = DefaultPaperWidthMm,
            DefaultPaperHeightMm = DefaultPaperHeightMm,
            DefaultOrientation = DefaultOrientation,
            AutoSaveEnabled = AutoSaveEnabled,
            AutoSaveIntervalSeconds = AutoSaveIntervalSeconds,
            ShowGrid = ShowGrid,
            GridSpacingMm = GridSpacingMm,
            SnapToGrid = SnapToGrid,
            DefaultBackgroundColor = DefaultBackgroundColor,
            NudgeDistanceMm = NudgeDistanceMm,
            LargeNudgeDistanceMm = LargeNudgeDistanceMm
        };
        AppSettingsService.Save(s);
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        var defaults = new AppSettings();
        Language = defaults.Language;
        PrintDpi = defaults.PrintDpi;
        DefaultPaperWidthMm = defaults.DefaultPaperWidthMm;
        DefaultPaperHeightMm = defaults.DefaultPaperHeightMm;
        DefaultOrientation = defaults.DefaultOrientation;
        AutoSaveEnabled = defaults.AutoSaveEnabled;
        AutoSaveIntervalSeconds = defaults.AutoSaveIntervalSeconds;
        ShowGrid = defaults.ShowGrid;
        GridSpacingMm = defaults.GridSpacingMm;
        SnapToGrid = defaults.SnapToGrid;
        DefaultBackgroundColor = defaults.DefaultBackgroundColor;
        NudgeDistanceMm = defaults.NudgeDistanceMm;
        LargeNudgeDistanceMm = defaults.LargeNudgeDistanceMm;
    }
}
