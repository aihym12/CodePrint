using System.Windows;
using System.Windows.Controls;
using CodePrint.ViewModels;
using Microsoft.Win32;

namespace CodePrint.Views.Panels;

public partial class PdfCropView : UserControl
{
    public PdfCropView()
    {
        InitializeComponent();
        Loaded += PdfCropView_Loaded;
    }

    private PdfCropViewModel ViewModel => (PdfCropViewModel)DataContext;

    private void PdfCropView_Loaded(object sender, RoutedEventArgs e)
    {
        // Restore processing mode radio buttons from persisted settings
        switch (ViewModel.ProcessingMode)
        {
            case PdfProcessingMode.PageCrop:
                PageCropRadio.IsChecked = true;
                break;
            case PdfProcessingMode.LabelSplit:
                LabelSplitRadio.IsChecked = true;
                break;
            case PdfProcessingMode.None:
                NoneRadio.IsChecked = true;
                break;
        }

        // Restore paper size radio buttons
        RestorePaperSizeRadio();

        // Restore density radio buttons
        RestoreDensityRadio();
    }

    private void RestorePaperSizeRadio()
    {
        var tag = ViewModel.SelectedPaperSizeIndex.ToString();
        foreach (var rb in FindVisualChildren<RadioButton>(this))
        {
            if (rb.GroupName == "PaperSize" && rb.Tag is string t && t == tag)
            {
                rb.IsChecked = true;
                break;
            }
        }
    }

    private void RestoreDensityRadio()
    {
        var tag = ViewModel.ImageDensity.ToString();
        foreach (var rb in FindVisualChildren<RadioButton>(this))
        {
            if (rb.GroupName == "Density" && rb.Tag is string t && t == tag)
            {
                rb.IsChecked = true;
                break;
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t)
                yield return t;
            foreach (var sub in FindVisualChildren<T>(child))
                yield return sub;
        }
    }

    private void SelectPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF文件 (*.pdf)|*.pdf",
            Title = "选择PDF文件"
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.LoadPdf(dialog.FileName);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = files != null && files.Length > 0 && files[0].EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0 && files[0].EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.LoadPdf(files[0]);
            }
        }
    }

    private void PageCropMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfCropViewModel vm)
            vm.ProcessingMode = PdfProcessingMode.PageCrop;
    }

    private void LabelSplitMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfCropViewModel vm)
            vm.ProcessingMode = PdfProcessingMode.LabelSplit;
    }

    private void NoneMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfCropViewModel vm)
            vm.ProcessingMode = PdfProcessingMode.None;
    }

    private void PaperSize_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && DataContext is PdfCropViewModel vm)
            vm.SetPaperSizeCommand.Execute(tag);
    }

    private void Density_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && DataContext is PdfCropViewModel vm)
            vm.SetDensityCommand.Execute(tag);
    }
}
