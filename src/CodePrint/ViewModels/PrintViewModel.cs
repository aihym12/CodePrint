using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Helpers;
using CodePrint.Models;

namespace CodePrint.ViewModels;

public partial class PrintViewModel : ObservableObject
{
    [ObservableProperty]
    private PrintSettings _settings = new();

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
        catch
        {
            // Fallback when print server is not available (e.g., running in restricted environment)
            AvailablePrinters.Add("Microsoft Print to PDF");
        }

        if (AvailablePrinters.Count > 0 && SelectedPrinter == null)
            SelectedPrinter = AvailablePrinters[0];
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

        try
        {
            var printDialog = new PrintDialog();

            // Configure the selected printer
            try
            {
                using var printServer = new LocalPrintServer();
                var queue = printServer.GetPrintQueue(SelectedPrinter);
                printDialog.PrintQueue = queue;
            }
            catch
            {
                // If the specific printer can't be found, fall back to system default printer
            }

            var mmToPx = DesignConstants.MmToPixel;
            int rows = Math.Max(1, Settings.LabelsPerRow);
            int cols = Math.Max(1, Settings.LabelsPerColumn);
            double labelW = Settings.PaperWidth * mmToPx;
            double labelH = Settings.PaperHeight * mmToPx;
            double pageW = labelW * cols;
            double pageH = labelH * rows;

            // Set the exact paper size so the printer uses the correct label dimensions
            printDialog.PrintTicket.PageMediaSize = new PageMediaSize(pageW, pageH);
            printDialog.PrintTicket.PageOrientation = Settings.Orientation == PrintOrientation.Landscape
                ? System.Printing.PageOrientation.Landscape
                : System.Printing.PageOrientation.Portrait;
            printDialog.PrintTicket.CopyCount = Settings.Copies;

            // Render document to a visual for printing
            var visual = RenderDocumentVisual();
            printDialog.PrintVisual(visual, $"CodePrint - {Document.Name}");

            StatusText = "打印任务已发送";
            RequestClose?.Invoke(true);
        }
        catch (Exception ex)
        {
            StatusText = $"打印失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Preview() => IsPreviewMode = !IsPreviewMode;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }

    /// <summary>Print resolution in DPI. Higher values produce sharper output on thermal printers like Qirui QR-488 (203 DPI native).
    /// Rendering at 600 DPI and letting the printer driver downsample produces significantly sharper text and barcodes.</summary>
    private const double PrintDpi = 600;

    /// <summary>Renders the current document into a visual element suitable for printing.</summary>
    private DrawingVisual RenderDocumentVisual()
    {
        var visual = new DrawingVisual();
        var mmToPx = DesignConstants.MmToPixel;

        double docWidth = Document!.WidthMm * mmToPx;
        double docHeight = Document.HeightMm * mmToPx;

        // Prepare label background brush
        var bgBrush = Document.BackgroundColor == "Transparent"
            ? Brushes.White
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Document.BackgroundColor));

        // Render label content to a bitmap at high DPI for sharp printing
        var canvas = new Canvas { Width = docWidth, Height = docHeight };
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

        // Render at high DPI for sharp print output (600 DPI instead of 96)
        double dpiScale = PrintDpi / 96.0;
        int bitmapWidth = (int)Math.Max(1, docWidth * dpiScale);
        int bitmapHeight = (int)Math.Max(1, docHeight * dpiScale);
        var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
            bitmapWidth, bitmapHeight, PrintDpi, PrintDpi, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);

        int rows = Math.Max(1, Settings.LabelsPerRow);
        int cols = Math.Max(1, Settings.LabelsPerColumn);
        double labelW = Settings.PaperWidth * mmToPx;
        double labelH = Settings.PaperHeight * mmToPx;

        using (var dc = visual.RenderOpen())
        {
            // Tile the label across the page for multi-label layouts
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    double x = c * labelW;
                    double y = r * labelH;

                    // Draw label background
                    dc.DrawRectangle(bgBrush, null, new Rect(x, y, labelW, labelH));

                    // Draw label content scaled to the label cell
                    dc.DrawImage(renderBitmap, new Rect(x, y, labelW, labelH));
                }
            }
        }

        return visual;
    }
}
