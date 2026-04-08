using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodePrint.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodePrint.ViewModels;

/// <summary>Processing mode for PDF crop and print.</summary>
public enum PdfProcessingMode
{
    PageCrop,
    LabelSplit,
    None
}

/// <summary>Sub-mode for page crop processing.</summary>
public enum CropMode
{
    Auto,
    Manual
}

/// <summary>Image density level for print output.</summary>
public enum DensityLevel
{
    Auto,
    Light,
    Medium,
    Dark
}

/// <summary>ViewModel for the PDF Crop &amp; Print module.</summary>
public partial class PdfCropViewModel : ObservableObject
{
    private readonly PdfRenderService _pdfService = new();
    private bool _isInitializing = true;

    public event Action? NavigateBack;

    public PdfCropViewModel()
    {
        LoadSettings();
        _isInitializing = false;
    }

    // ── File state ──

    [ObservableProperty]
    private string? _pdfFilePath;

    [ObservableProperty]
    private bool _isFileLoaded;

    [ObservableProperty]
    private BitmapSource? _previewImage;

    // ── Step tracking ──

    [ObservableProperty]
    private bool _isNextStepVisible;

    // ── Processing mode (step 1) ──

    [ObservableProperty]
    private PdfProcessingMode _processingMode = PdfProcessingMode.PageCrop;

    partial void OnProcessingModeChanged(PdfProcessingMode value) => SaveSettings();

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

    // ── Page navigation ──

    [ObservableProperty]
    private int _currentPageIndex;

    [ObservableProperty]
    private int _totalPages;

    public string PageInfoText => TotalPages > 0
        ? $"{CurrentPageIndex + 1}/{TotalPages}页"
        : "0/0页";

    partial void OnCurrentPageIndexChanged(int value) => OnPropertyChanged(nameof(PageInfoText));
    partial void OnTotalPagesChanged(int value) => OnPropertyChanged(nameof(PageInfoText));

    // ── Paper size (step 2) ──

    [ObservableProperty]
    private int _selectedPaperSizeIndex;

    partial void OnSelectedPaperSizeIndexChanged(int value) => SaveSettings();

    [ObservableProperty]
    private double _paperWidthMm = 100;

    partial void OnPaperWidthMmChanged(double value) => SaveSettings();

    [ObservableProperty]
    private double _paperHeightMm = 100;

    partial void OnPaperHeightMmChanged(double value) => SaveSettings();

    [ObservableProperty]
    private bool _isCustomSize;

    partial void OnIsCustomSizeChanged(bool value) => SaveSettings();

    [ObservableProperty]
    private double _customWidthMm = 80;

    partial void OnCustomWidthMmChanged(double value)
    {
        if (IsCustomSize)
            PaperWidthMm = value;
        SaveSettings();
    }

    [ObservableProperty]
    private double _customHeightMm = 60;

    partial void OnCustomHeightMmChanged(double value)
    {
        if (IsCustomSize)
            PaperHeightMm = value;
        SaveSettings();
    }

    // ── Print layout ──

    [ObservableProperty]
    private int _printLayoutIndex;

    partial void OnPrintLayoutIndexChanged(int value) => SaveSettings();

    [ObservableProperty]
    private bool _applyCropToAllPages = true;

    partial void OnApplyCropToAllPagesChanged(bool value) => SaveSettings();

    // ── Image density ──

    [ObservableProperty]
    private DensityLevel _imageDensity = DensityLevel.Auto;

    partial void OnImageDensityChanged(DensityLevel value)
    {
        SaveSettings();
        // Re-render preview at new density when file is loaded
        if (IsFileLoaded && IsNextStepVisible)
            _ = RenderCurrentPageAsync();
    }

    // ── Last printer (persisted but not displayed in this VM) ──

    public string? LastPrinterName { get; set; }

    // ── Status ──

    [ObservableProperty]
    private string _statusText = "请选择或拖入PDF文件";

    // ── Settings persistence ──

    private void LoadSettings()
    {
        var s = PdfCropSettingsService.Load();
        _selectedPaperSizeIndex = s.SelectedPaperSizeIndex;
        _paperWidthMm = s.PaperWidthMm;
        _paperHeightMm = s.PaperHeightMm;
        _isCustomSize = s.IsCustomSize;
        _customWidthMm = s.CustomWidthMm;
        _customHeightMm = s.CustomHeightMm;
        _printLayoutIndex = s.PrintLayoutIndex;
        _applyCropToAllPages = s.ApplyCropToAllPages;
        _imageDensity = Enum.TryParse<DensityLevel>(s.ImageDensity, out var d) ? d : DensityLevel.Auto;
        _processingMode = Enum.TryParse<PdfProcessingMode>(s.ProcessingMode, out var m) ? m : PdfProcessingMode.PageCrop;
        LastPrinterName = s.LastPrinterName;
    }

    private void SaveSettings()
    {
        if (_isInitializing) return;
        PdfCropSettingsService.Save(new PdfCropSettings
        {
            SelectedPaperSizeIndex = SelectedPaperSizeIndex,
            PaperWidthMm = PaperWidthMm,
            PaperHeightMm = PaperHeightMm,
            IsCustomSize = IsCustomSize,
            CustomWidthMm = CustomWidthMm,
            CustomHeightMm = CustomHeightMm,
            PrintLayoutIndex = PrintLayoutIndex,
            ApplyCropToAllPages = ApplyCropToAllPages,
            ImageDensity = ImageDensity.ToString(),
            ProcessingMode = ProcessingMode.ToString(),
            LastPrinterName = LastPrinterName
        });
    }

    // ── Commands ──

    [RelayCommand]
    private async Task GoToNextStep()
    {
        if (!IsFileLoaded)
        {
            StatusText = "请先选择PDF文件";
            return;
        }
        IsNextStepVisible = true;
        await RenderCurrentPageAsync();
    }

    [RelayCommand]
    private void GoToPreviousStep()
    {
        IsNextStepVisible = false;
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
            _pdfService.Close();
            NavigateBack?.Invoke();
        }
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPageIndex > 0)
        {
            CurrentPageIndex--;
            await RenderCurrentPageAsync();
        }
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPageIndex < TotalPages - 1)
        {
            CurrentPageIndex++;
            await RenderCurrentPageAsync();
        }
    }

    [RelayCommand]
    private void SetPaperSize(string tag)
    {
        switch (tag)
        {
            case "0":
                SelectedPaperSizeIndex = 0;
                PaperWidthMm = 100; PaperHeightMm = 100;
                IsCustomSize = false;
                break;
            case "1":
                SelectedPaperSizeIndex = 1;
                PaperWidthMm = 100; PaperHeightMm = 150;
                IsCustomSize = false;
                break;
            case "2":
                SelectedPaperSizeIndex = 2;
                PaperWidthMm = 58; PaperHeightMm = 40;
                IsCustomSize = false;
                break;
            case "3":
                SelectedPaperSizeIndex = 3;
                IsCustomSize = true;
                PaperWidthMm = CustomWidthMm;
                PaperHeightMm = CustomHeightMm;
                break;
        }
    }

    [RelayCommand]
    private void SetDensity(string tag)
    {
        ImageDensity = tag switch
        {
            "Auto" => DensityLevel.Auto,
            "Light" => DensityLevel.Light,
            "Medium" => DensityLevel.Medium,
            "Dark" => DensityLevel.Dark,
            _ => DensityLevel.Auto
        };
    }

    [RelayCommand]
    private async Task Print()
    {
        if (!IsFileLoaded || PreviewImage == null)
        {
            StatusText = "请先选择PDF文件";
            return;
        }

        var dialog = new Views.Dialogs.PdfPrintDialog(TotalPages, LastPrinterName)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true)
        {
            StatusText = "已取消打印";
            return;
        }

        var printerName = dialog.ResultPrinterName;
        var pageFrom = dialog.ResultPageFrom;
        var pageTo = dialog.ResultPageTo;
        var copies = dialog.ResultCopies;

        // Remember the printer for next time
        LastPrinterName = printerName;
        SaveSettings();

        StatusText = "正在准备打印数据…";

        try
        {
            var printDialog = new PrintDialog();

            // Set the selected printer
            try
            {
                using var printServer = new LocalPrintServer();
                var queue = printServer.GetPrintQueue(printerName);
                printDialog.PrintQueue = queue;
            }
            catch
            {
                // Fall back to system default if queue lookup fails
            }

            printDialog.PrintTicket.CopyCount = copies;

            var columns = PrintLayoutIndex + 1; // 0=单排(1col), 1=双排(2col), 2=三排(3col)
            var mmToPx = 96.0 / 25.4;
            var labelW = PaperWidthMm * mmToPx;
            var labelH = PaperHeightMm * mmToPx;
            var pageW = labelW * columns;
            var pageH = labelH;

            // Set the exact paper size so the printer knows the label dimensions
            printDialog.PrintTicket.PageMediaSize =
                new PageMediaSize(pageW, pageH);

            // For thermal printers: ensure portrait orientation (width = printhead, height = feed)
            printDialog.PrintTicket.PageOrientation = System.Printing.PageOrientation.Portrait;

            var startIdx = pageFrom - 1;
            var endIdx = pageTo - 1;

            StatusText = $"正在渲染 {endIdx - startIdx + 1} 页…";
            var doc = new FixedDocument();

            if (columns == 1)
            {
                // Single column: one page per PDF page
                for (int i = startIdx; i <= endIdx; i++)
                {
                    var page = await CreateFixedPageAsync(i, labelW, labelH);
                    var content = new PageContent();
                    ((IAddChild)content).AddChild(page);
                    doc.Pages.Add(content);
                }
            }
            else
            {
                // Multi-column: group PDF pages into rows
                var pageIndices = new List<int>();
                for (int i = startIdx; i <= endIdx; i++)
                    pageIndices.Add(i);

                for (int g = 0; g < pageIndices.Count; g += columns)
                {
                    var fixedPage = new FixedPage { Width = pageW, Height = pageH };
                    var dpi = GetPrintDpi();

                    for (int c = 0; c < columns && g + c < pageIndices.Count; c++)
                    {
                        var img = await _pdfService.RenderPageAsync(pageIndices[g + c], dpi);
                        if (img != null)
                        {
                            var rect = FitImageToRect(img, labelW, labelH);
                            var imgCtrl = new System.Windows.Controls.Image
                            {
                                Source = img,
                                Width = rect.Width,
                                Height = rect.Height,
                                Stretch = Stretch.Uniform
                            };
                            RenderOptions.SetBitmapScalingMode(imgCtrl, BitmapScalingMode.HighQuality);
                            FixedPage.SetLeft(imgCtrl, c * labelW + rect.X);
                            FixedPage.SetTop(imgCtrl, rect.Y);
                            fixedPage.Children.Add(imgCtrl);
                        }
                    }

                    fixedPage.Measure(new Size(pageW, pageH));
                    fixedPage.Arrange(new Rect(0, 0, pageW, pageH));
                    fixedPage.UpdateLayout();

                    var content = new PageContent();
                    ((IAddChild)content).AddChild(fixedPage);
                    doc.Pages.Add(content);
                }
            }

            StatusText = "正在发送到打印机…";
            printDialog.PrintDocument(doc.DocumentPaginator,
                $"CodePrint PDF - {Path.GetFileName(PdfFilePath)}");

            StatusText = "打印任务已发送";
        }
        catch (Exception ex)
        {
            StatusText = $"打印失败: {ex.Message}";
        }
    }

    // ── Public methods ──

    public async void LoadPdf(string filePath)
    {
        try
        {
            PdfFilePath = filePath;
            StatusText = "正在加载PDF…";

            await _pdfService.LoadAsync(filePath);

            TotalPages = _pdfService.PageCount;
            CurrentPageIndex = 0;
            IsFileLoaded = true;
            StatusText = $"已加载: {Path.GetFileName(filePath)}";

            await RenderCurrentPageAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败: {ex.Message}";
            IsFileLoaded = false;
        }
    }

    // ── Private helpers ──

    private double GetRenderDpi()
    {
        return ImageDensity switch
        {
            DensityLevel.Light => 96,
            DensityLevel.Medium => 200,
            DensityLevel.Dark => 300,
            _ => 200  // Auto — good preview quality
        };
    }

    private double GetPrintDpi()
    {
        return ImageDensity switch
        {
            DensityLevel.Light => 300,
            DensityLevel.Medium => 600,
            DensityLevel.Dark => 600,
            _ => 600  // Auto — high quality for sharp text on thermal printers like QR-488
        };
    }

    private async Task RenderCurrentPageAsync()
    {
        if (!_pdfService.IsLoaded || CurrentPageIndex < 0 || CurrentPageIndex >= TotalPages)
            return;

        try
        {
            var dpi = GetRenderDpi();
            PreviewImage = await _pdfService.RenderPageAsync(CurrentPageIndex, dpi);
        }
        catch (Exception ex)
        {
            StatusText = $"渲染失败: {ex.Message}";
        }
    }

    private async Task<FixedPage> CreateFixedPageAsync(int pageIndex, double targetW, double targetH)
    {
        var page = new FixedPage { Width = targetW, Height = targetH };

        var dpi = GetPrintDpi();
        var img = await _pdfService.RenderPageAsync(pageIndex, dpi);

        if (img != null)
        {
            var rect = FitImageToRect(img, targetW, targetH);
            var imgCtrl = new System.Windows.Controls.Image
            {
                Source = img,
                Width = rect.Width,
                Height = rect.Height,
                Stretch = Stretch.Uniform
            };
            RenderOptions.SetBitmapScalingMode(imgCtrl, BitmapScalingMode.HighQuality);
            FixedPage.SetLeft(imgCtrl, rect.X);
            FixedPage.SetTop(imgCtrl, rect.Y);
            page.Children.Add(imgCtrl);
        }

        page.Measure(new Size(targetW, targetH));
        page.Arrange(new Rect(0, 0, targetW, targetH));
        page.UpdateLayout();
        return page;
    }

    private static Rect FitImageToRect(BitmapSource img, double boxW, double boxH)
    {
        var scaleX = boxW / img.PixelWidth;
        var scaleY = boxH / img.PixelHeight;
        var scale = Math.Min(scaleX, scaleY);
        var w = img.PixelWidth * scale;
        var h = img.PixelHeight * scale;
        return new Rect((boxW - w) / 2, (boxH - h) / 2, w, h);
    }
}
