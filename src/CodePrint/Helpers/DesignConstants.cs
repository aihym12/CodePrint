namespace CodePrint.Helpers;

/// <summary>
/// Application-wide design constants.
/// </summary>
public static class DesignConstants
{
    /// <summary>Conversion factor: 1mm = 96/25.4 WPF pixels</summary>
    public const double MmToPixel = 96.0 / 25.4;

    /// <summary>Minimum zoom level</summary>
    public const double MinZoom = 0.25;

    /// <summary>Maximum zoom level</summary>
    public const double MaxZoom = 10.0;

    /// <summary>Zoom increment per step</summary>
    public const double ZoomStep = 0.25;

    /// <summary>Snap threshold in pixels for alignment guides</summary>
    public const double SnapThreshold = 5.0;

    /// <summary>Arrow key move distance in mm</summary>
    public const double NudgeDistance = 0.265;

    /// <summary>Shift+Arrow move distance in mm</summary>
    public const double LargeNudgeDistance = 2.65;

    /// <summary>Resize handle size in pixels</summary>
    public const double HandleSize = 8.0;

    /// <summary>Rotation handle offset above element in pixels</summary>
    public const double RotationHandleOffset = 20.0;

    /// <summary>Default canvas background color</summary>
    public const string DefaultCanvasBackground = "#F0F0F0";

    /// <summary>Default label background color</summary>
    public const string DefaultLabelBackground = "#FFFFFF";

    /// <summary>Grid line spacing in mm</summary>
    public const double GridSpacing = 5.0;

    /// <summary>Primary theme color (tomato orange)</summary>
    public const string PrimaryColor = "#E53935";

    /// <summary>Secondary theme color</summary>
    public const string SecondaryColor = "#FF8C00";
}
