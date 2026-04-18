using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;

namespace CodePrint.ViewModels;

public partial class PrintViewModel : ObservableObject
{
    [ObservableProperty]
    private PrintSettings _settings = PrintSettingsService.Load();

    [ObservableProperty]
    private ObservableCollection<string> _availablePrinters = new();

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private LabelDocument? _document;

    [ObservableProperty]
    private bool _isPreviewMode;

    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>边距提示文本。</summary>
    [ObservableProperty]
    private string _marginHintText = string.Empty;

    partial void OnSettingsChanged(PrintSettings value)
    {
        UpdateMarginHint();
    }

    /// <summary>Updates the margin hint text from the current settings.</summary>
    public void UpdateMarginHint()
    {
        MarginHintText = $"目前空出了 {Settings.PrintMarginPx} 像素";
    }

    /// <summary>Raised when the dialog should close with a success result.</summary>
    public event Action<bool>? RequestClose;

    public int TotalLabels => Settings.LabelsPerRow * Settings.LabelsPerColumn;

    [RelayCommand]
    private void RefreshPrinters()
    {
        AvailablePrinters.Clear();
        try
        {
            using var printServer = new LocalPrintServer();
            var queues = printServer.GetPrintQueues();
            foreach (var queue in queues)
            {
                AvailablePrinters.Add(queue.Name);
            }
        }
        catch (Exception ex)
        {
            // 在受限环境（无打印权限/无 spooler）下回退到虚拟打印机；
            // 但务必把原因记到日志，便于排查"为什么列表里只有一台打印机"。
            Debug.WriteLine($"[Print] 枚举打印机失败: {ex}");
            StatusText = "无法访问本地打印服务，已回退到虚拟打印机";
            AvailablePrinters.Add("Microsoft Print to PDF");
        }

        if (AvailablePrinters.Count > 0)
        {
            // Restore previously saved printer if available
            if (!string.IsNullOrEmpty(Settings.PrinterName) && AvailablePrinters.Contains(Settings.PrinterName))
                SelectedPrinter = Settings.PrinterName;
            else if (SelectedPrinter == null)
                SelectedPrinter = AvailablePrinters[0];
        }
    }

    [RelayCommand]
    private void Print()
    {
        if (Document == null || string.IsNullOrEmpty(SelectedPrinter))
        {
            StatusText = "请选择打印机并确保文档已加载";
            return;
        }

        Settings.PrinterName = SelectedPrinter;
        SaveSettings();

        // 打印服务器需要在整个打印过程中保持存活，因为后续要调用
        // PrintQueue.GetPrintCapabilities 以及 PrintDialog.PrintDocument，
        // 它们都依赖底层的 COM 句柄。提前 Dispose 会出现"访问已释放对象"
        // 的难以排查的偶发异常。
        LocalPrintServer? printServer = null;
        try
        {
            var printDialog = new PrintDialog();

            // Configure the selected printer
            try
            {
                printServer = new LocalPrintServer();
                var queue = printServer.GetPrintQueue(SelectedPrinter);
                printDialog.PrintQueue = queue;
            }
            catch (Exception ex)
            {
                // 找不到指定打印机时回退到系统默认打印机；记录原因便于诊断。
                Debug.WriteLine($"[Print] 无法获取打印队列 '{SelectedPrinter}': {ex.Message}");
            }

            // ── 单位说明 ──
            // WPF 打印管线统一使用"设备无关像素"（DIP，1 DIP = 1/96 英寸），
            // PrintTicket.PageMediaSize 也是 DIP 单位。
            // DesignConstants.MmToPixel == 96/25.4，因此 mm * MmToPixel 得到的就是 DIP，
            // 和 PageMediaSize 所需单位一致，无需再次换算。
            var mmToDip = DesignConstants.MmToPixel;
            int rows = Math.Max(1, Settings.LabelsPerRow);
            int cols = Math.Max(1, Settings.LabelsPerColumn);
            double labelW = Settings.PaperWidth * mmToDip;
            double labelH = Settings.PaperHeight * mmToDip;
            double pageW = labelW * cols;
            double pageH = labelH * rows;

            // Set the exact paper size so the printer uses the correct label dimensions
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(pageW, pageH);
            printDialog.PrintTicket.PageOrientation = Settings.Orientation == PrintOrientation.Landscape
                ? System.Printing.PageOrientation.Landscape
                : System.Printing.PageOrientation.Portrait;
            printDialog.PrintTicket.CopyCount = Settings.Copies;

            // 取打印机可印区原点和范围。
            // 原点（OriginWidth/Height）是相对于物理页面左上角的偏移
            // （通常是几毫米的不可印边缘），整页**只发生一次**，绝不能再按
            // 行列均摊到每一张子标签上——之前的实现 originX/cols、originY/rows
            // 是错误的，会让中间几列的标签错位。
            // 范围（ExtentWidth/Height）是可印区的实际宽高，套准标志要画在
            // 这一矩形的四角上，而不是简单假设左右/上下边距对称。
            double originX = 0, originY = 0;
            double extentW = pageW, extentH = pageH;
            try
            {
                var caps = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
                if (caps.PageImageableArea != null)
                {
                    originX = caps.PageImageableArea.OriginWidth;
                    originY = caps.PageImageableArea.OriginHeight;
                    if (caps.PageImageableArea.ExtentWidth > 0)
                        extentW = caps.PageImageableArea.ExtentWidth;
                    if (caps.PageImageableArea.ExtentHeight > 0)
                        extentH = caps.PageImageableArea.ExtentHeight;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Print] 读取打印机可印区失败，按零偏移处理: {ex.Message}");
            }

            // User-configured print margin (shrink content on all sides)
            double margin = Math.Max(0, Settings.PrintMarginPx);

            // Render label content to a high-DPI bitmap
            var renderBitmap = RenderLabelBitmap();

            // Build a FixedDocument. Place content at (margin, margin) with
            // reduced dimensions so all content (including border lines) fits
            // within the printable area after the WPF pipeline shift.
            var fixedPage = new FixedPage { Width = pageW, Height = pageH };

            // 每张标签的可用宽高：仅扣除用户边距；可印区原点的整体偏移
            // 在最外层做一次平移即可，不需要逐标签扣除。
            double availLabelW = Math.Max(1, labelW - 2 * margin);
            double availLabelH = Math.Max(1, labelH - 2 * margin);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var img = new Image
                    {
                        Source = renderBitmap,
                        Width = availLabelW,
                        Height = availLabelH,
                        Stretch = Stretch.Uniform
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

                    // 整体偏移：可印区原点 + 用户边距 + 该标签在网格中的位置。
                    double left = originX + c * labelW + margin;
                    double top = originY + r * labelH + margin;

                    // Mirror print: flip content horizontally
                    if (Settings.MirrorPrint)
                    {
                        img.RenderTransformOrigin = new Point(0.5, 0.5);
                        img.RenderTransform = new ScaleTransform(-1, 1);
                    }

                    FixedPage.SetLeft(img, left);
                    FixedPage.SetTop(img, top);
                    fixedPage.Children.Add(img);

                    // Draw cut/border lines around each label
                    if (Settings.ShowCutLines)
                    {
                        var border = new System.Windows.Shapes.Rectangle
                        {
                            Width = availLabelW,
                            Height = availLabelH,
                            Stroke = Brushes.Black,
                            StrokeThickness = 0.5,
                            StrokeDashArray = new DoubleCollection { 4, 2 },
                            Fill = Brushes.Transparent
                        };
                        FixedPage.SetLeft(border, left);
                        FixedPage.SetTop(border, top);
                        fixedPage.Children.Add(border);
                    }
                }
            }

            // 打印套准（registration / 校准）标志：在可印区四角绘制 L 形黑色短线，
            // 供后道贴标/分切机识别对齐位置。该设置之前已在 PrintSettings 中定义
            // 但完全未生效，这里补全实现。
            if (Settings.EnableRegistrationPrint)
            {
                AddRegistrationMarks(fixedPage, originX, originY, extentW, extentH);
            }

            fixedPage.Measure(new Size(pageW, pageH));
            fixedPage.Arrange(new Rect(0, 0, pageW, pageH));
            fixedPage.UpdateLayout();

            var fixedDoc = new FixedDocument();
            fixedDoc.DocumentPaginator.PageSize = new Size(pageW, pageH);
            var pageContent = new PageContent { Child = fixedPage };
            fixedDoc.Pages.Add(pageContent);

            printDialog.PrintDocument(fixedDoc.DocumentPaginator, $"CodePrint - {Document.Name}");

            StatusText = "打印任务已发送";
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Print] 打印失败: {ex}");
            StatusText = $"打印失败: {ex.Message}";
        }
        finally
        {
            printServer?.Dispose();
        }
    }

    /// <summary>
    /// 在可印区的四个角绘制 L 形套准（registration）标志，便于后道工序对齐。
    /// 标志长度 5mm、线宽 0.3mm。坐标参数为可印区原点和实际宽高，避免依赖
    /// 上下/左右边距对称的假设。
    /// </summary>
    private static void AddRegistrationMarks(FixedPage fixedPage, double originX, double originY, double extentW, double extentH)
    {
        var mmToDip = DesignConstants.MmToPixel;
        double markLen = 5.0 * mmToDip;
        double thickness = 0.3 * mmToDip;

        double x0 = originX;
        double y0 = originY;
        double x1 = originX + extentW;
        double y1 = originY + extentH;

        // 八条短线：每个角两条（横+竖），构成 L 形。
        var corners = new (double x, double y, double dx, double dy)[]
        {
            (x0, y0,  1,  0), (x0, y0,  0,  1), // 左上
            (x1, y0, -1,  0), (x1, y0,  0,  1), // 右上
            (x0, y1,  1,  0), (x0, y1,  0, -1), // 左下
            (x1, y1, -1,  0), (x1, y1,  0, -1), // 右下
        };

        foreach (var (x, y, dx, dy) in corners)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = x,
                Y1 = y,
                X2 = x + dx * markLen,
                Y2 = y + dy * markLen,
                Stroke = Brushes.Black,
                StrokeThickness = thickness,
                SnapsToDevicePixels = true
            };
            fixedPage.Children.Add(line);
        }
    }

    [RelayCommand]
    private void Preview() => IsPreviewMode = !IsPreviewMode;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    /// <summary>Persists the current settings to disk so they are restored next time.</summary>
    public void SaveSettings()
    {
        if (!string.IsNullOrEmpty(SelectedPrinter))
            Settings.PrinterName = SelectedPrinter;
        PrintSettingsService.Save(Settings);
    }

    /// <summary>常见 DPI 选项。</summary>
    public IReadOnlyList<int> DpiOptions { get; } = PrintConstants.StandardDpiOptions;

    /// <summary>Print resolution in DPI. Uses per-print setting if set, otherwise falls back to global app setting.</summary>
    private int PrintDpi => Settings.PrintDpi > 0 ? Settings.PrintDpi : AppSettingsService.Current.PrintDpi;

    /// <summary>Renders the label content to a high-DPI bitmap for printing.</summary>
    private RenderTargetBitmap RenderLabelBitmap()
    {
        var mmToPx = DesignConstants.MmToPixel;

        double docWidth = Document!.WidthMm * mmToPx;
        double docHeight = Document.HeightMm * mmToPx;

        // Prepare label background brush.
        // 背景色字符串来自文档（用户可编辑），有可能是非法值，必须容错。
        Brush bgBrush;
        if (string.IsNullOrEmpty(Document.BackgroundColor) || Document.BackgroundColor == "Transparent")
        {
            bgBrush = Brushes.White;
        }
        else
        {
            try
            {
                bgBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Document.BackgroundColor));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Print] 背景色 '{Document.BackgroundColor}' 解析失败，回退为白色: {ex.Message}");
                bgBrush = Brushes.White;
            }
        }

        // Render label content to a bitmap at high DPI for sharp printing
        var canvas = new Canvas { Width = docWidth, Height = docHeight, Background = bgBrush };
        foreach (var element in Document.Elements.OrderBy(e => e.ZIndex))
        {
            if (!element.IsVisible) continue;
            CanvasRendererHelper.RenderElement(canvas, element);
        }

        // Enable high-quality text rendering for print output
        // Use Grayscale anti-aliasing instead of ClearType (ClearType is designed for LCD subpixels, not printers)
        TextOptions.SetTextRenderingMode(canvas, TextRenderingMode.Grayscale);
        TextOptions.SetTextFormattingMode(canvas, TextFormattingMode.Ideal);
        TextOptions.SetTextHintingMode(canvas, TextHintingMode.Fixed);
        RenderOptions.SetBitmapScalingMode(canvas, BitmapScalingMode.HighQuality);
        canvas.UseLayoutRounding = true;
        canvas.SnapsToDevicePixels = true;

        canvas.Measure(new Size(docWidth, docHeight));
        canvas.Arrange(new Rect(0, 0, docWidth, docHeight));
        canvas.UpdateLayout();

        // Render at high DPI for sharp print output (e.g. 300 DPI instead of 96)
        double dpiScale = PrintDpi / 96.0;
        int bitmapWidth = (int)Math.Max(1, docWidth * dpiScale);
        int bitmapHeight = (int)Math.Max(1, docHeight * dpiScale);
        var renderBitmap = new RenderTargetBitmap(
            bitmapWidth, bitmapHeight, PrintDpi, PrintDpi, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);

        return renderBitmap;
    }
}
