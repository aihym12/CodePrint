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

    /// <summary>Renders the current document into a visual element suitable for printing.</summary>
    private DrawingVisual RenderDocumentVisual()
    {
        var visual = new DrawingVisual();
        var mmToPx = DesignConstants.MmToPixel;

        using (var dc = visual.RenderOpen())
        {
            double docWidth = Document!.WidthMm * mmToPx;
            double docHeight = Document.HeightMm * mmToPx;

            // Draw background
            var bgBrush = Document.BackgroundColor == "Transparent"
                ? Brushes.White
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString(Document.BackgroundColor));
            dc.DrawRectangle(bgBrush, null, new Rect(0, 0, docWidth, docHeight));

            // Render each element by drawing it onto a temporary canvas and then rendering it
            var canvas = new Canvas { Width = docWidth, Height = docHeight };
            foreach (var element in Document.Elements.OrderBy(e => e.ZIndex))
            {
                if (!element.IsVisible) continue;
                CanvasRendererHelper.RenderElement(canvas, element);
            }

            // Measure and arrange the canvas so it can be rendered
            canvas.Measure(new Size(docWidth, docHeight));
            canvas.Arrange(new Rect(0, 0, docWidth, docHeight));
            canvas.UpdateLayout();

            // Render the canvas onto the drawing context
            var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)docWidth, (int)docHeight, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(canvas);
            dc.DrawImage(renderBitmap, new Rect(0, 0, docWidth, docHeight));
        }

        return visual;
    }
}
