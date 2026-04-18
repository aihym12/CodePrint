using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Helpers;
using CodePrint.Models;
using CodePrint.Services;
using CodePrint.Services.Printing;

namespace CodePrint.ViewModels;

public partial class PrintViewModel : ObservableObject
{
    /// <summary>底层打印服务（默认 GDI 实现，可注入替换为 ZPL/TSPL/Mock）。</summary>
    private readonly IPrintService _printService;

    /// <summary>当前作业的取消源。仅在打印进行中非空。</summary>
    private CancellationTokenSource? _activeCts;

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

    /// <summary>打印进度 [0,1]；&lt;0 表示未知/未开始。</summary>
    [ObservableProperty]
    private double _printProgress = -1;

    /// <summary>是否正在打印（用于禁用/启用按钮、显示取消按钮）。</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrintCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelPrintCommand))]
    private bool _isPrinting;

    /// <summary>边距提示文本。</summary>
    [ObservableProperty]
    private string _marginHintText = string.Empty;

    /// <summary>XAML 用的无参构造：使用默认 GDI 实现。</summary>
    public PrintViewModel() : this(new GdiPrintService()) { }

    /// <summary>注入式构造：测试或切换为 ZPL/TSPL 等实现时使用。</summary>
    public PrintViewModel(IPrintService printService)
    {
        _printService = printService ?? throw new ArgumentNullException(nameof(printService));
    }

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
    private async Task RefreshPrintersAsync()
    {
        AvailablePrinters.Clear();
        IReadOnlyList<PrinterInfo> printers;
        try
        {
            printers = await _printService.DiscoverPrintersAsync();
        }
        catch (Exception ex)
        {
            // IPrintService 约定不抛异常；这里只做防御。
            Debug.WriteLine($"[PrintVM] 枚举打印机抛出异常: {ex}");
            printers = Array.Empty<PrinterInfo>();
        }

        if (printers.Count == 0)
        {
            // 受限环境（无 spooler / 无权限）下回退到虚拟打印机；
            // 用户可见提示 + 日志，避免出现"为什么列表里只有一个"的困惑。
            StatusText = "无法访问本地打印服务，已回退到虚拟打印机";
            AvailablePrinters.Add("Microsoft Print to PDF");
        }
        else
        {
            foreach (var p in printers)
                AvailablePrinters.Add(p.Name);
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

    private bool CanPrint() => !IsPrinting;

    [RelayCommand(CanExecute = nameof(CanPrint))]
    private async Task PrintAsync()
    {
        if (Document == null || string.IsNullOrEmpty(SelectedPrinter))
        {
            StatusText = "请选择打印机并确保文档已加载";
            return;
        }

        Settings.PrinterName = SelectedPrinter;
        SaveSettings();

        var request = new PrintJobRequest(
            Document,
            Settings,
            SelectedPrinter,
            JobName: $"CodePrint - {Document.Name}");

        var progress = new Progress<PrintProgress>(p =>
        {
            StatusText = p.Message;
            PrintProgress = p.Fraction;
        });

        _activeCts = new CancellationTokenSource();
        IsPrinting = true;
        try
        {
            var result = await _printService.PrintAsync(request, progress, _activeCts.Token);

            if (result.Success)
            {
                StatusText = "打印任务已发送";
                RequestClose?.Invoke(true);
            }
            else
            {
                // 取消和失败的状态文字已在 progress 中给出；此处只做兜底。
                StatusText = result.ErrorMessage ?? "打印未完成";
            }
        }
        finally
        {
            IsPrinting = false;
            PrintProgress = -1;
            _activeCts.Dispose();
            _activeCts = null;
        }
    }

    private bool CanCancelPrint() => IsPrinting;

    [RelayCommand(CanExecute = nameof(CanCancelPrint))]
    private void CancelPrint()
    {
        if (TryCancelActiveJob())
            StatusText = "正在取消…";
    }

    [RelayCommand]
    private void Preview() => IsPreviewMode = !IsPreviewMode;

    [RelayCommand]
    private void Cancel()
    {
        // 关闭对话框前若仍在打印，先尝试取消作业。
        TryCancelActiveJob();
        RequestClose?.Invoke(false);
    }

    /// <summary>
    /// 尝试取消当前活动的打印作业。返回是否真正发起了取消。
    /// 集中处理 <see cref="ObjectDisposedException"/> 的竞态，避免散落在多处。
    /// </summary>
    private bool TryCancelActiveJob()
    {
        var cts = _activeCts;
        if (cts == null) return false;
        try
        {
            cts.Cancel();
            return true;
        }
        catch (ObjectDisposedException)
        {
            // 作业已在另一个线程结束并释放了 CTS——忽略
            return false;
        }
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
}
