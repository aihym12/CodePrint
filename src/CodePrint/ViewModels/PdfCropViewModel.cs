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
    Dark,
    Custom
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
        UpdateDensityHint();
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
    private DensityLevel _imageDensity = DensityLevel.Dark;

    /// <summary>Hint text showing the effective print DPI for the current density level.</summary>
    [ObservableProperty]
    private string _densityHintText = "";

    /// <summary>自定义 DPI 值，仅当 ImageDensity 为 Custom 时使用。</summary>
    [ObservableProperty]
    private int _customDpi = 300;

    /// <summary>是否显示自定义 DPI 输入框。</summary>
    public bool IsCustomDensity => ImageDensity == DensityLevel.Custom;

    partial void OnCustomDpiChanged(int value)
    {
        SaveSettings();
        UpdateDensityHint();
        if (IsFileLoaded && IsNextStepVisible)
            _ = RenderCurrentPageAsync();
    }

    partial void OnImageDensityChanged(DensityLevel value)
    {
        SaveSettings();
        UpdateDensityHint();
        OnPropertyChanged(nameof(IsCustomDensity));
        // Re-render preview at new density when file is loaded
        if (IsFileLoaded && IsNextStepVisible)
            _ = RenderCurrentPageAsync();
    }

    private void UpdateDensityHint()
    {
        var dpi = GetPrintDpi();
        DensityHintText = ImageDensity switch
        {
            DensityLevel.Auto => $"自适应打印 DPI: {dpi}（根据打印机自动选择）",
            DensityLevel.Light => $"打印 DPI: {dpi}（适合草稿快速打印）",
            DensityLevel.Medium => $"打印 DPI: {dpi}（标准质量）",
            DensityLevel.Dark => $"打印 DPI: {dpi}（高清质量）",
            DensityLevel.Custom => $"自定义打印 DPI: {dpi}",
            _ => ""
        };
    }

    // ── Last printer (persisted but not displayed in this VM) ──

    public string? LastPrinterName { get; set; }

    // ── Print margin ──

    /// <summary>打印边距（像素），上下左右各缩进该值。</summary>
    [ObservableProperty]
    private double _printMarginPx = 5;

    /// <summary>边距提示文本。</summary>
    [ObservableProperty]
    private string _marginHintText = "目前空出了 5 像素";

    partial void OnPrintMarginPxChanged(double value)
    {
        MarginHintText = $"目前空出了 {value} 像素";
        SaveSettings();
    }

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
        _imageDensity = Enum.TryParse<DensityLevel>(s.ImageDensity, out var d) ? d : DensityLevel.Dark;
        _customDpi = s.CustomDpi;
        _processingMode = Enum.TryParse<PdfProcessingMode>(s.ProcessingMode, out var m) ? m : PdfProcessingMode.PageCrop;
        LastPrinterName = s.LastPrinterName;
        _printMarginPx = s.PrintMarginPx;
        _marginHintText = $"目前空出了 {_printMarginPx} 像素";
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
            CustomDpi = CustomDpi,
            ProcessingMode = ProcessingMode.ToString(),
            LastPrinterName = LastPrinterName,
            PrintMarginPx = PrintMarginPx
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
            "Custom" => DensityLevel.Custom,
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

        var dialog = new Views.Dialogs.PdfPrintDialog(TotalPages, LastPrinterName, (int)GetPrintDpi())
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
        var overrideDpi = dialog.ResultDpi;

        // Remember the printer for next time
        LastPrinterName = printerName;
        // If user specified a custom DPI in the print dialog, apply it
        if (overrideDpi > 0)
        {
            CustomDpi = overrideDpi;
            ImageDensity = DensityLevel.Custom;
        }
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

            var startIdx = pageFrom - 1;
            var endIdx = pageTo - 1;

            StatusText = $"正在渲染 {endIdx - startIdx + 1} 页…";
            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(pageW, pageH);

            if (columns == 1)
            {
                // Single column: one page per PDF page
                for (int i = startIdx; i <= endIdx; i++)
                {
                    var page = await CreateFixedPageAsync(i, labelW, labelH, originX, originY);
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

                // Apply user-configured margin on all sides
                double margin = Math.Max(0, PrintMarginPx);

                // Distribute the printable width evenly across columns.
                // The printer adds originX as a left margin, so the total
                // available width on paper is (pageW - originX - 2*margin).
                double totalAvailW = Math.Max(1, pageW - originX - 2 * margin);
                double availColW = totalAvailW / columns;
                double availH = Math.Max(1, pageH - originY - 2 * margin);

                for (int g = 0; g < pageIndices.Count; g += columns)
                {
                    var fixedPage = new FixedPage { Width = pageW, Height = pageH };
                    var dpi = GetPrintDpi();

                    for (int c = 0; c < columns && g + c < pageIndices.Count; c++)
                    {
                        var img = await RenderPageForPrintAsync(pageIndices[g + c], dpi);
                        if (img != null)
                        {
                            var imgCtrl = new System.Windows.Controls.Image
                            {
                                Source = img,
                                Width = availColW,
                                Height = availH,
                                Stretch = Stretch.Uniform,
                                UseLayoutRounding = true,
                                SnapsToDevicePixels = true
                            };
                            RenderOptions.SetBitmapScalingMode(imgCtrl, BitmapScalingMode.HighQuality);
                            FixedPage.SetLeft(imgCtrl, margin + c * availColW);
                            FixedPage.SetTop(imgCtrl, margin);
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
            DensityLevel.Light => 72,
            DensityLevel.Medium => 150,
            DensityLevel.Dark => 200,
            DensityLevel.Custom => Math.Max(72, Math.Min(CustomDpi, 300)),  // Preview DPI clamped to 72~300 for performance
            _ => 150  // Auto — balanced preview quality
        };
    }

    private double GetPrintDpi()
    {
        return ImageDensity switch
        {
            DensityLevel.Light => 300,
            DensityLevel.Medium => 600,
            DensityLevel.Dark => 1200,
            DensityLevel.Custom => Math.Max(72, Math.Min(CustomDpi, 1200)),  // Print DPI clamped to 72~1200
            _ => GetAutoPrintDpi()  // Auto — detect from printer
        };
    }

    /// <summary>Attempt to read the printer's native DPI; fall back to 300 if unavailable.</summary>
    private double GetAutoPrintDpi()
    {
        try
        {
            if (!string.IsNullOrEmpty(LastPrinterName))
            {
                using var server = new LocalPrintServer();
                var queue = server.GetPrintQueue(LastPrinterName);
                var ticket = queue.DefaultPrintTicket;
                var res = ticket.PageResolution;
                if (res != null)
                {
                    var detectedDpi = Math.Max(res.X ?? 0, res.Y ?? 0);
                    if (detectedDpi > 0) return detectedDpi;
                }
            }
        }
        catch (Exception)
        {
            // Printer not available or doesn't report resolution; fall back to default
        }
        return 600; // Safe default for most thermal printers
    }

    private async Task RenderCurrentPageAsync()
    {
        if (!_pdfService.IsLoaded || CurrentPageIndex < 0 || CurrentPageIndex >= TotalPages)
            return;

        try
        {
            var dpi = GetRenderDpi();

            if (ProcessingMode == PdfProcessingMode.PageCrop && IsNextStepVisible)
            {
                // Show cropped preview so the user sees exactly what will be printed
                var crop = await GetCropRectAsync(CurrentPageIndex);
                PreviewImage = await _pdfService.RenderPageCroppedAsync(
                    CurrentPageIndex, dpi, crop.X, crop.Y, crop.Width, crop.Height);
            }
            else
            {
                PreviewImage = await _pdfService.RenderPageAsync(CurrentPageIndex, dpi);
            }
        }
        catch (Exception ex)
        {
            StatusText = $"渲染失败: {ex.Message}";
        }
    }

    /// <summary>
    /// Calculates the crop rectangle in millimeters for the given page based on
    /// the current processing mode and crop settings.
    /// For Auto mode: detects the content bounding box (trims whitespace) so all
    /// content is preserved; the print/preview pipeline scales the result to fit the paper.
    /// For Manual mode: uses user-specified CropX/Y/Width/Height.
    /// </summary>
    private async Task<(double X, double Y, double Width, double Height)> GetCropRectAsync(int pageIndex)
    {
        var (pageWidthMm, pageHeightMm) = _pdfService.GetPageSizeMm(pageIndex);

        if (CropMode == CropMode.Manual)
        {
            // Use manual crop settings, clamped to page bounds
            double x = Math.Max(0, Math.Min(CropX, pageWidthMm));
            double y = Math.Max(0, Math.Min(CropY, pageHeightMm));
            double w = Math.Max(1, Math.Min(CropWidth, pageWidthMm - x));
            double h = Math.Max(1, Math.Min(CropHeight, pageHeightMm - y));
            return (x, y, w, h);
        }

        // Auto mode: detect content bounds (trim whitespace) so only the
        // actual content area is rendered. Stretch.Uniform then scales it
        // to fit within the selected paper size.
        return await _pdfService.GetContentBoundsMmAsync(pageIndex);
    }

    /// <summary>Renders a PDF page for printing, applying crop when in PageCrop mode.</summary>
    private async Task<BitmapSource?> RenderPageForPrintAsync(int pageIndex, double dpi)
    {
        if (ProcessingMode == PdfProcessingMode.PageCrop)
        {
            var crop = await GetCropRectAsync(pageIndex);
            return await _pdfService.RenderPageCroppedAsync(
                pageIndex, dpi, crop.X, crop.Y, crop.Width, crop.Height);
        }

        return await _pdfService.RenderPageAsync(pageIndex, dpi);
    }

    private async Task<FixedPage> CreateFixedPageAsync(int pageIndex, double pageW, double pageH, double originX, double originY)
    {
        var page = new FixedPage { Width = pageW, Height = pageH };

        var dpi = GetPrintDpi();
        var img = await RenderPageForPrintAsync(pageIndex, dpi);

        if (img != null)
        {
            // Fit content within the printer's imageable (printable) area,
            // then apply the user-configured margin on all four sides.
            double margin = Math.Max(0, PrintMarginPx);
            double availW = Math.Max(1, pageW - originX - 2 * margin);
            double availH = Math.Max(1, pageH - originY - 2 * margin);
            var imgCtrl = new System.Windows.Controls.Image
            {
                Source = img,
                Width = availW,
                Height = availH,
                Stretch = Stretch.Uniform,
                UseLayoutRounding = true,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(imgCtrl, BitmapScalingMode.HighQuality);
            FixedPage.SetLeft(imgCtrl, margin);
            FixedPage.SetTop(imgCtrl, margin);
            page.Children.Add(imgCtrl);
        }

        page.Measure(new Size(pageW, pageH));
        page.Arrange(new Rect(0, 0, pageW, pageH));
        page.UpdateLayout();
        return page;
    }
}
