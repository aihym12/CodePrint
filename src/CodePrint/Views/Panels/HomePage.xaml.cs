using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CodePrint.Models;
using CodePrint.ViewModels;
using CodePrint.Views.Dialogs;
using Microsoft.Win32;

namespace CodePrint.Views.Panels;

public partial class HomePage : UserControl
{
    private static int _importCounter;

    public HomePage()
    {
        InitializeComponent();
    }

    private HomeViewModel ViewModel => (HomeViewModel)DataContext;

    private void NewBlankLabel_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new NewLabelDialog { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.ConfirmNewLabel(dialog.CreatedDocument);
        }
    }

    private void PdfCropPrint_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenPdfCropCommand.Execute(null);
    }

    private void PhotoPrint_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenPhotoPrintCommand.Execute(null);
    }

    private void ImportImageTemplate_Click(object sender, MouseButtonEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要导入的照片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif|所有文件|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        var imagePath = dialog.FileName;

        // Read image dimensions
        double pixelWidth, pixelHeight, dpiX, dpiY;
        try
        {
            using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            var frame = decoder.Frames[0];
            pixelWidth = frame.PixelWidth;
            pixelHeight = frame.PixelHeight;
            dpiX = frame.DpiX > 0 ? frame.DpiX : 96;
            dpiY = frame.DpiY > 0 ? frame.DpiY : 96;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            MessageBox.Show("无法读取图片文件，请检查文件格式是否正确。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Convert pixel dimensions to mm using the image's DPI
        var widthMm = pixelWidth / dpiX * 25.4;
        var heightMm = pixelHeight / dpiY * 25.4;

        // Clamp to reasonable label size: max 300mm, min 10mm
        const double maxMm = 300.0;
        const double minMm = 10.0;
        if (widthMm > maxMm || heightMm > maxMm)
        {
            var scale = Math.Min(maxMm / widthMm, maxMm / heightMm);
            widthMm *= scale;
            heightMm *= scale;
        }
        widthMm = Math.Max(widthMm, minMm);
        heightMm = Math.Max(heightMm, minMm);

        // Round to 1 decimal place
        widthMm = Math.Round(widthMm, 1);
        heightMm = Math.Round(heightMm, 1);

        _importCounter++;
        var document = new LabelDocument
        {
            Name = $"照片模板_{_importCounter}",
            WidthMm = widthMm,
            HeightMm = heightMm,
            Orientation = widthMm > heightMm ? PrintOrientation.Landscape : PrintOrientation.Portrait
        };

        // Add an ImageElement that fills the entire label
        var imageElement = new ImageElement
        {
            Name = Path.GetFileNameWithoutExtension(imagePath),
            ImagePath = imagePath,
            X = 0,
            Y = 0,
            Width = widthMm,
            Height = heightMm,
            MaintainAspectRatio = true,
            IsLocked = false,
            ZIndex = 0
        };
        document.Elements.Add(imageElement);

        ViewModel.ConfirmNewLabel(document);
    }
}
