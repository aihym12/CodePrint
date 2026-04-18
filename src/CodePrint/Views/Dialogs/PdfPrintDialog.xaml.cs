using System.Collections.ObjectModel;
using System.Printing;
using System.Windows;
using CodePrint.Helpers;

namespace CodePrint.Views.Dialogs;

public partial class PdfPrintDialog : Window
{
    public PdfPrintDialog(int totalPages, string? lastPrinterName = null, int lastDpi = 0)
    {
        InitializeComponent();
        DataContext = this;

        PageFrom = 1;
        PageTo = totalPages;
        TotalPages = totalPages;
        Copies = 1;
        PrintDpi = lastDpi;

        LoadPrinters(lastPrinterName);
    }

    // ── Bindable properties ──

    public ObservableCollection<string> Printers { get; } = new();

    /// <summary>常见 DPI 选项（包含 0 = 使用默认值）。</summary>
    public IReadOnlyList<int> DpiOptions { get; } = PrintConstants.DpiOptionsWithDefault;

    public string? SelectedPrinter
    {
        get => _selectedPrinter;
        set { _selectedPrinter = value; }
    }
    private string? _selectedPrinter;

    public int PageFrom { get; set; }
    public int PageTo { get; set; }
    public int TotalPages { get; set; }
    public int Copies { get; set; }
    public int PrintDpi { get; set; }

    // ── Result properties (read after DialogResult == true) ──

    public string ResultPrinterName => SelectedPrinter ?? string.Empty;
    public int ResultPageFrom => Math.Max(1, Math.Min(PageFrom, TotalPages));
    public int ResultPageTo => Math.Max(ResultPageFrom, Math.Min(PageTo, TotalPages));
    public int ResultCopies => Math.Max(1, Copies);
    public int ResultDpi => Math.Max(0, PrintDpi);

    // ── Private ──

    private void LoadPrinters(string? lastPrinterName)
    {
        Printers.Clear();
        try
        {
            using var printServer = new LocalPrintServer();
            var queues = printServer.GetPrintQueues();
            foreach (var queue in queues)
            {
                Printers.Add(queue.Name);
            }
        }
        catch (Exception ex)
        {
            // 受限环境下回退到虚拟打印机；记录原因方便排查。
            System.Diagnostics.Debug.WriteLine($"[PdfPrintDialog] 枚举打印机失败: {ex.Message}");
            Printers.Add("Microsoft Print to PDF");
        }

        // Restore last used printer if available
        if (!string.IsNullOrEmpty(lastPrinterName) && Printers.Contains(lastPrinterName))
            SelectedPrinter = lastPrinterName;
        else if (Printers.Count > 0)
            SelectedPrinter = Printers[0];
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedPrinter))
        {
            MessageBox.Show("请选择打印机", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void AddCloudPrinter_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("云打印机功能即将推出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
