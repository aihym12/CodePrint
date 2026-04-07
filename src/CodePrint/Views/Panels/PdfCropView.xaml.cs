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
    }

    private PdfCropViewModel ViewModel => (PdfCropViewModel)DataContext;

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

    private void AutoCrop_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfCropViewModel vm)
            vm.CropMode = CropMode.Auto;
    }

    private void ManualCrop_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PdfCropViewModel vm)
            vm.CropMode = CropMode.Manual;
    }
}
