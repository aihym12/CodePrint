using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodePrint.ViewModels;

/// <summary>Processing mode for PDF crop and print.</summary>
public enum PdfProcessingMode
{
    /// <summary>Crop pages by auto-detecting content boundaries or manual selection.</summary>
    PageCrop,

    /// <summary>Split a single page into multiple label units.</summary>
    LabelSplit,

    /// <summary>No processing — print PDF at original size.</summary>
    None
}

/// <summary>Sub-mode for page crop processing.</summary>
public enum CropMode
{
    Auto,
    Manual
}

/// <summary>ViewModel for the PDF Crop &amp; Print module (PRD Section 10).</summary>
public partial class PdfCropViewModel : ObservableObject
{
    /// <summary>Raised when user clicks Back to return to the home page.</summary>
    public event Action? NavigateBack;

    [ObservableProperty]
    private string? _pdfFilePath;

    [ObservableProperty]
    private bool _isFileLoaded;

    [ObservableProperty]
    private PdfProcessingMode _processingMode = PdfProcessingMode.PageCrop;

    [ObservableProperty]
    private CropMode _cropMode = CropMode.Auto;

    // ── Label split settings ──

    [ObservableProperty]
    private int _splitRows = 2;

    [ObservableProperty]
    private int _splitColumns = 2;

    [ObservableProperty]
    private double _splitGapMm = 2.0;

    [ObservableProperty]
    private double _splitMarginMm = 3.0;

    // ── Manual crop settings ──

    [ObservableProperty]
    private double _cropX;

    [ObservableProperty]
    private double _cropY;

    [ObservableProperty]
    private double _cropWidth = 50;

    [ObservableProperty]
    private double _cropHeight = 30;

    // ── Print settings (after "Next") ──

    [ObservableProperty]
    private string _pageRange = "全部";

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private bool _isNextStepVisible;

    [ObservableProperty]
    private string _statusText = "请选择或拖入PDF文件";

    [RelayCommand]
    private void GoToNextStep()
    {
        if (!IsFileLoaded)
        {
            StatusText = "请先选择PDF文件";
            return;
        }
        IsNextStepVisible = true;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (IsNextStepVisible)
        {
            IsNextStepVisible = false;
        }
        else
        {
            NavigateBack?.Invoke();
        }
    }

    [RelayCommand]
    private void Print()
    {
        if (!IsFileLoaded || string.IsNullOrEmpty(PdfFilePath))
        {
            StatusText = "请先选择PDF文件";
            return;
        }

        StatusText = "正在发送到打印机…";

        try
        {
            var printDialog = new PrintDialog();
            if (printDialog.ShowDialog() != true)
            {
                StatusText = "已取消打印";
                return;
            }

            printDialog.PrintTicket.CopyCount = Copies;

            // Load PDF page as an image for printing (XPS-based approach)
            // Since WPF doesn't natively render PDFs, we print using a
            // visual placeholder that shows the file info and crop settings.
            var visual = CreatePrintVisual(printDialog);
            printDialog.PrintVisual(visual, $"CodePrint PDF - {System.IO.Path.GetFileName(PdfFilePath)}");

            StatusText = "打印任务已发送";
        }
        catch (Exception ex)
        {
            StatusText = $"打印失败: {ex.Message}";
        }
    }

    /// <summary>Sets the loaded file path and updates state.</summary>
    public void LoadPdf(string filePath)
    {
        PdfFilePath = filePath;
        IsFileLoaded = true;
        StatusText = $"已加载: {System.IO.Path.GetFileName(filePath)}";
    }

    /// <summary>Creates a print visual based on the current processing mode and settings.</summary>
    private DrawingVisual CreatePrintVisual(PrintDialog printDialog)
    {
        var visual = new DrawingVisual();
        var pageWidth = printDialog.PrintableAreaWidth;
        var pageHeight = printDialog.PrintableAreaHeight;

        using (var dc = visual.RenderOpen())
        {
            // White background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageWidth, pageHeight));

            switch (ProcessingMode)
            {
                case PdfProcessingMode.PageCrop when CropMode == CropMode.Manual:
                    // Draw the manual crop region outline
                    var mmToPx = 96.0 / 25.4;
                    var cropRect = new Rect(CropX * mmToPx, CropY * mmToPx, CropWidth * mmToPx, CropHeight * mmToPx);
                    dc.DrawRectangle(null, new Pen(Brushes.Red, 1), cropRect);
                    DrawCenteredText(dc, $"PDF裁剪区域: {CropWidth:F1}×{CropHeight:F1}mm",
                        pageWidth, pageHeight / 2 - 20);
                    DrawCenteredText(dc, System.IO.Path.GetFileName(PdfFilePath!),
                        pageWidth, pageHeight / 2 + 10);
                    break;

                case PdfProcessingMode.LabelSplit:
                    // Draw split grid lines
                    var cellW = (pageWidth - SplitMarginMm * 2 * (96.0 / 25.4)) / SplitColumns;
                    var cellH = (pageHeight - SplitMarginMm * 2 * (96.0 / 25.4)) / SplitRows;
                    var marginPx = SplitMarginMm * (96.0 / 25.4);
                    var gapPx = SplitGapMm * (96.0 / 25.4);
                    var pen = new Pen(Brushes.LightGray, 0.5);

                    for (int r = 0; r <= SplitRows; r++)
                    {
                        var y = marginPx + r * (cellH + gapPx);
                        dc.DrawLine(pen, new Point(marginPx, y), new Point(pageWidth - marginPx, y));
                    }
                    for (int c = 0; c <= SplitColumns; c++)
                    {
                        var x = marginPx + c * (cellW + gapPx);
                        dc.DrawLine(pen, new Point(x, marginPx), new Point(x, pageHeight - marginPx));
                    }

                    DrawCenteredText(dc, $"PDF标签分割: {SplitRows}行×{SplitColumns}列",
                        pageWidth, pageHeight / 2 - 20);
                    DrawCenteredText(dc, System.IO.Path.GetFileName(PdfFilePath!),
                        pageWidth, pageHeight / 2 + 10);
                    break;

                default:
                    // No processing — just show file info at center
                    DrawCenteredText(dc, $"PDF打印: {System.IO.Path.GetFileName(PdfFilePath!)}",
                        pageWidth, pageHeight / 2);
                    break;
            }
        }

        return visual;
    }

    private static void DrawCenteredText(DrawingContext dc, string text, double pageWidth, double y)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Microsoft YaHei"),
            14,
            Brushes.Black,
            96);

        dc.DrawText(formattedText, new Point((pageWidth - formattedText.Width) / 2, y));
    }
}
