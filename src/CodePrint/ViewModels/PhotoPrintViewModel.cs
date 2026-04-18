using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CodePrint.Helpers;

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
    public IReadOnlyList<int> DpiOptions { get; } = PrintConstants.StandardDpiOptions;

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

            int printedCount = 0;
            int skippedCount = 0;

            foreach (var photo in Photos)
            {
                BitmapImage? bitmap = TryLoadBitmap(photo);
                if (bitmap == null)
                {
                    // 图片加载失败时直接跳过：不能把"无法加载图片"作为
                    // 正常一页送进打印机，否则会浪费纸张和耗材。
                    skippedCount++;
                    continue;
                }

                for (int copy = 0; copy < photo.Copies; copy++)
                {
                    try
                    {
                        var visual = RenderPhotoVisual(photo, bitmap, pageWidth, pageHeight);
                        printDialog.PrintVisual(visual, $"CodePrint Photo - {photo.FileName}");
                        printedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[PhotoPrint] 打印 {photo.FileName} 失败: {ex}");
                        StatusText = $"打印 {photo.FileName} 失败: {ex.Message}";
                        return;
                    }
                }
            }

            if (skippedCount == 0)
                StatusText = $"打印任务已发送（{printedCount} 张）";
            else
                StatusText = $"打印任务已发送（{printedCount} 张），{skippedCount} 张图片加载失败已跳过";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhotoPrint] 打印失败: {ex}");
            StatusText = $"打印失败: {ex.Message}";
        }
    }

    /// <summary>
    /// 尝试从 <see cref="PhotoItem.FilePath"/> 加载位图；失败时返回 null。
    /// </summary>
    private static BitmapImage? TryLoadBitmap(PhotoItem photo)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(photo.FilePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PhotoPrint] 加载图片失败 '{photo.FilePath}': {ex.Message}");
            return null;
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

    /// <summary>
    /// 把单张照片渲染为可打印的 Visual。位图由调用方预先加载并传入，
    /// 这样图片加载失败可以由 <see cref="Print"/> 统一跳过该项，而不是在这里
    /// 静默地把"无法加载图片"作为一页打出来。
    /// </summary>
    private DrawingVisual RenderPhotoVisual(PhotoItem photo, BitmapImage bitmap, double pageWidth, double pageHeight)
    {
        var visual = new DrawingVisual();

        // Set high-quality rendering hints for sharp print output
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);

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
        int dpi = PrintDpi > 0 ? PrintDpi : (int)PrintConstants.WpfDpi;
        double dpiScale = dpi / PrintConstants.WpfDpi;
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
