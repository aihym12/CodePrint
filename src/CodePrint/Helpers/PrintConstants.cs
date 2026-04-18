namespace CodePrint.Helpers;

/// <summary>
/// 打印相关的全局常量。集中维护，避免在多处出现重复字面量。
/// </summary>
public static class PrintConstants
{
    /// <summary>
    /// 标准 DPI 选项。
    /// 150：低速热敏；203：常见标签机；300：高精度热转印；600：照片/精细图文。
    /// </summary>
    public static readonly int[] StandardDpiOptions = { 150, 203, 300, 600 };

    /// <summary>
    /// 包含 "0（使用默认值）" 在内的 DPI 选项，供允许"跟随全局设置"的对话框使用。
    /// </summary>
    public static readonly int[] DpiOptionsWithDefault = { 0, 150, 203, 300, 600 };

    /// <summary>WPF 默认的设备无关像素 DPI（1 DIP = 1/96 英寸）。</summary>
    public const double WpfDpi = 96.0;
}
