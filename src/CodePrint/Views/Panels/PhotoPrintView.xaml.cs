using System.Windows;
using System.Windows.Controls;
using CodePrint.ViewModels;
using Microsoft.Win32;

namespace CodePrint.Views.Panels;

public partial class PhotoPrintView : UserControl
{
    private static readonly string ImageFilter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff;*.tif|所有文件|*.*";

    public PhotoPrintView()
    {
        InitializeComponent();
    }

    private PhotoPrintViewModel ViewModel => (PhotoPrintViewModel)DataContext;

    private void AddPhotos_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = ImageFilter,
            Title = "选择图片文件",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.AddFiles(dialog.FileNames);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnFileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                ViewModel.AddFiles(files);
            }
        }
    }

    private void FitMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PhotoPrintViewModel vm)
            vm.ScaleMode = PhotoScaleMode.Fit;
    }

    private void FillMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PhotoPrintViewModel vm)
            vm.ScaleMode = PhotoScaleMode.Fill;
    }

    private void StretchMode_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PhotoPrintViewModel vm)
            vm.ScaleMode = PhotoScaleMode.Stretch;
    }
}
