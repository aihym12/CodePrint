using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CodePrint.ViewModels;

/// <summary>Scaling mode for photo printing.</summary>
public enum PhotoScaleMode
{
    /// <summary>Fill the label area (may crop edges).</summary>
    Fill,

    /// <summary>Fit within label maintaining aspect ratio (may have borders).</summary>
    Fit,

    /// <summary>Stretch to exactly fill the label (may distort).</summary>
    Stretch
}

/// <summary>Represents a single photo queued for printing.</summary>
public partial class PhotoItem : ObservableObject
{
    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private double _rotation;

    [ObservableProperty]
    private double _brightness = 1.0;

    [ObservableProperty]
    private double _contrast = 1.0;

    public string FileName => System.IO.Path.GetFileName(FilePath);
}

/// <summary>ViewModel for the Photo Print module (PRD Section 11).</summary>
public partial class PhotoPrintViewModel : ObservableObject
{
    /// <summary>Raised when user clicks Back to return to the home page.</summary>
    public event Action? NavigateBack;

    [ObservableProperty]
    private ObservableCollection<PhotoItem> _photos = new();

    [ObservableProperty]
    private PhotoItem? _selectedPhoto;

    [ObservableProperty]
    private PhotoScaleMode _scaleMode = PhotoScaleMode.Fit;

    [ObservableProperty]
    private string _statusText = "请选择或拖入图片文件";

    /// <summary>打印 DPI（清晰度），0 表示使用默认 96 DPI。</summary>
    [ObservableProperty]
    private int _printDpi = 300;

    /// <summary>常见 DPI 选项。</summary>
    public IReadOnlyList<int> DpiOptions { get; } = new[] { 150, 203, 300, 600 };

    [RelayCommand]
    private void RemovePhoto()
    {
        if (SelectedPhoto != null)
        {
            Photos.Remove(SelectedPhoto);
            SelectedPhoto = Photos.Count > 0 ? Photos[0] : null;
        }
    }

    [RelayCommand]
    private void ClearPhotos()
    {
        Photos.Clear();
        SelectedPhoto = null;
        StatusText = "请选择或拖入图片文件";
    }

    [RelayCommand]
    private void RotateClockwise()
    {
        if (SelectedPhoto != null)
            SelectedPhoto.Rotation = (SelectedPhoto.Rotation + 90) % 360;
    }

    [RelayCommand]
    private void RotateCounterClockwise()
    {
        if (SelectedPhoto != null)
            SelectedPhoto.Rotation = (SelectedPhoto.Rotation + 270) % 360;
    }

    [RelayCommand]
    private void GoBack()
    {
        NavigateBack?.Invoke();
    }

    [RelayCommand]
    private void Print()
    {
        if (Photos.Count == 0)
        {
            StatusText = "请先添加图片";
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

            var pageWidth = printDialog.PrintableAreaWidth;
            var pageHeight = printDialog.PrintableAreaHeight;

            foreach (var photo in Photos)
            {
                for (int copy = 0; copy < photo.Copies; copy++)
                {
                    try
                    {
                        var visual = RenderPhotoVisual(photo, pageWidth, pageHeight);
                        printDialog.PrintVisual(visual, $"CodePrint Photo - {photo.FileName}");
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"打印 {photo.FileName} 失败: {ex.Message}";
                        return;
                    }
                }
            }

            StatusText = "打印任务已发送";
        }
        catch (Exception ex)
        {
            StatusText = $"打印失败: {ex.Message}";
        }
    }

    /// <summary>Adds images from file paths.</summary>
    public void AddFiles(string[] filePaths)
    {
        foreach (var path in filePaths)
        {
            Photos.Add(new PhotoItem { FilePath = path });
        }
        if (SelectedPhoto == null && Photos.Count > 0)
            SelectedPhoto = Photos[0];
        StatusText = $"已添加 {Photos.Count} 张图片";
    }

    /// <summary>Renders a single photo item into a print visual at the specified DPI.</summary>
    private DrawingVisual RenderPhotoVisual(PhotoItem photo, double pageWidth, double pageHeight)
    {
        var visual = new DrawingVisual();

        // Set high-quality rendering hints for sharp print output
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

        // Load the image at high DPI for sharp print output
        BitmapImage bitmap;
        try
        {
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(photo.FilePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.EndInit();
            bitmap.Freeze();
        }
        catch
        {
            // Image file may be corrupted or unsupported; show a placeholder message instead
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageWidth, pageHeight));
                var errorText = new FormattedText(
                    $"无法加载图片: {photo.FileName}",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Microsoft YaHei"), 14, Brushes.Red, 96);
                dc.DrawText(errorText, new Point(20, pageHeight / 2));
            }
            return visual;
        }

        // Create a canvas with the image and apply transformations
        var canvas = new Canvas { Width = pageWidth, Height = pageHeight };
        canvas.Background = Brushes.White;

        double imgWidth = bitmap.PixelWidth;
        double imgHeight = bitmap.PixelHeight;

        // Calculate target rect based on scale mode
        Rect targetRect;
        switch (ScaleMode)
        {
            case PhotoScaleMode.Fill:
            {
                var scaleX = pageWidth / imgWidth;
                var scaleY = pageHeight / imgHeight;
                var scale = Math.Max(scaleX, scaleY);
                var scaledW = imgWidth * scale;
                var scaledH = imgHeight * scale;
                targetRect = new Rect(
                    (pageWidth - scaledW) / 2,
                    (pageHeight - scaledH) / 2,
                    scaledW, scaledH);
                canvas.ClipToBounds = true;
                break;
            }
            case PhotoScaleMode.Stretch:
            {
                targetRect = new Rect(0, 0, pageWidth, pageHeight);
                break;
            }
            default: // Fit
            {
                var scaleX = pageWidth / imgWidth;
                var scaleY = pageHeight / imgHeight;
                var scale = Math.Min(scaleX, scaleY);
                var scaledW = imgWidth * scale;
                var scaledH = imgHeight * scale;
                targetRect = new Rect(
                    (pageWidth - scaledW) / 2,
                    (pageHeight - scaledH) / 2,
                    scaledW, scaledH);
                break;
            }
        }

        var imgCtrl = new System.Windows.Controls.Image
        {
            Source = bitmap,
            Width = targetRect.Width,
            Height = targetRect.Height,
            Stretch = Stretch.Fill
        };
        RenderOptions.SetBitmapScalingMode(imgCtrl, BitmapScalingMode.HighQuality);

        if (photo.Rotation != 0)
        {
            imgCtrl.RenderTransformOrigin = new Point(0.5, 0.5);
            imgCtrl.RenderTransform = new RotateTransform(photo.Rotation);
        }

        Canvas.SetLeft(imgCtrl, targetRect.X);
        Canvas.SetTop(imgCtrl, targetRect.Y);
        canvas.Children.Add(imgCtrl);

        canvas.Measure(new Size(pageWidth, pageHeight));
        canvas.Arrange(new Rect(0, 0, pageWidth, pageHeight));
        canvas.UpdateLayout();

        // Render at specified DPI for sharp print output
        int dpi = PrintDpi > 0 ? PrintDpi : 96;
        double dpiScale = dpi / 96.0;
        int bitmapWidth = (int)Math.Max(1, pageWidth * dpiScale);
        int bitmapHeight = (int)Math.Max(1, pageHeight * dpiScale);
        var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
            bitmapWidth, bitmapHeight, dpi, dpi, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);

        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(renderBitmap, new Rect(0, 0, pageWidth, pageHeight));
        }

        return visual;
    }
}
