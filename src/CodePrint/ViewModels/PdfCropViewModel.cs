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
    private void SelectPdfFile()
    {
        // Handled in code-behind to open file dialog
    }

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
        StatusText = "正在发送到打印机…";
    }

    /// <summary>Sets the loaded file path and updates state.</summary>
    public void LoadPdf(string filePath)
    {
        PdfFilePath = filePath;
        IsFileLoaded = true;
        StatusText = $"已加载: {System.IO.Path.GetFileName(filePath)}";
    }
}
