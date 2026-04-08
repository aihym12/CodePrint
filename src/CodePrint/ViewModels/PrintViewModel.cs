using System.Collections.ObjectModel;
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

            // Get the printer's imageable-area origin so we can compensate for
            // the offset that the WPF printing pipeline applies.  Without this
            // compensation the content is shifted down/right by (OriginWidth,
            // OriginHeight), which pushes the bottom/right off the physical page
            // and leaves visible blank space.
            double originX = 0, originY = 0;
            try
            {
                var caps = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
                if (caps.PageImageableArea != null)
                {
                    originX = caps.PageImageableArea.OriginWidth;
                    originY = caps.PageImageableArea.OriginHeight;
                }
            }
            catch
            {
                // If capabilities are unavailable, assume zero offset
            }

            // Render label content to a high-DPI bitmap
            var renderBitmap = RenderLabelBitmap();

            // Build a FixedDocument.  Position content at (-originX, -originY)
            // so that after the printing pipeline adds the imageable-area offset,
            // the net position is (0,0) on the physical paper – filling the page
            // from edge to edge.
            var fixedPage = new FixedPage { Width = pageW, Height = pageH };

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var img = new Image
                    {
                        Source = renderBitmap,
                        Width = labelW,
                        Height = labelH,
                        Stretch = Stretch.Fill
                    };
                    FixedPage.SetLeft(img, c * labelW - originX);
                    FixedPage.SetTop(img, r * labelH - originY);
                    fixedPage.Children.Add(img);
                }
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

    /// <summary>Persists the current settings to disk so they are restored next time.</summary>
    public void SaveSettings()
    {
        if (!string.IsNullOrEmpty(SelectedPrinter))
            Settings.PrinterName = SelectedPrinter;
        PrintSettingsService.Save(Settings);
    }

    /// <summary>常见 DPI 选项。</summary>
    public IReadOnlyList<int> DpiOptions { get; } = new[] { 150, 203, 300, 600 };

    /// <summary>Print resolution in DPI. Uses per-print setting if set, otherwise falls back to global app setting.</summary>
    private int PrintDpi => Settings.PrintDpi > 0 ? Settings.PrintDpi : AppSettingsService.Current.PrintDpi;

    /// <summary>Renders the label content to a high-DPI bitmap for printing.</summary>
    private RenderTargetBitmap RenderLabelBitmap()
    {
        var mmToPx = DesignConstants.MmToPixel;

        double docWidth = Document!.WidthMm * mmToPx;
        double docHeight = Document.HeightMm * mmToPx;

        // Prepare label background brush
        var bgBrush = Document.BackgroundColor == "Transparent"
            ? Brushes.White
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Document.BackgroundColor));

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
