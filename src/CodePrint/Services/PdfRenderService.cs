using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CodePrint.Services;

/// <summary>
/// Renders PDF pages to WPF BitmapSource using the Windows.Data.Pdf API.
/// </summary>
public class PdfRenderService
{
    private PdfDocument? _pdfDocument;

    public int PageCount => (int)(_pdfDocument?.PageCount ?? 0);
    public bool IsLoaded => _pdfDocument != null;

    /// <summary>Loads a PDF file for rendering.</summary>
    public async Task LoadAsync(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var file = await StorageFile.GetFileFromPathAsync(fullPath);
        _pdfDocument = await PdfDocument.LoadFromFileAsync(file);
    }

    /// <summary>Renders a single page to a BitmapSource at the specified DPI.</summary>
    public async Task<BitmapSource> RenderPageAsync(int pageIndex, double dpi = 150)
    {
        if (_pdfDocument == null)
            throw new InvalidOperationException("PDF未加载");

        using var page = _pdfDocument.GetPage((uint)pageIndex);
        var stream = new InMemoryRandomAccessStream();

        // PdfPage.Size is in DIPs (96 per inch)
        var scale = dpi / 96.0;
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = (uint)Math.Max(1, page.Size.Width * scale),
            DestinationHeight = (uint)Math.Max(1, page.Size.Height * scale),
            BackgroundColor = new Windows.UI.Color { A = 255, R = 255, G = 255, B = 255 }
        };

        await page.RenderToStreamAsync(stream, options);
        stream.Seek(0);

        // Read rendered PNG into byte array via DataReader
        using var reader = new DataReader(stream);
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[stream.Size];
        reader.ReadBytes(bytes);

        // Create frozen BitmapImage for cross-thread safety
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        stream.Dispose();
        return bitmap;
    }

    /// <summary>
    /// Renders a cropped region of a PDF page to a BitmapSource at the specified DPI.
    /// The crop rectangle is specified in millimeters from the top-left corner of the page.
    /// </summary>
    public async Task<BitmapSource> RenderPageCroppedAsync(int pageIndex, double dpi,
        double cropXMm, double cropYMm, double cropWidthMm, double cropHeightMm)
    {
        if (_pdfDocument == null)
            throw new InvalidOperationException("PDF未加载");

        using var page = _pdfDocument.GetPage((uint)pageIndex);
        var stream = new InMemoryRandomAccessStream();

        // Convert crop rect from mm to DIPs (1 DIP = 1/96 inch, 1 inch = 25.4mm)
        const double mmToDip = 96.0 / 25.4;
        double srcX = cropXMm * mmToDip;
        double srcY = cropYMm * mmToDip;
        double srcW = cropWidthMm * mmToDip;
        double srcH = cropHeightMm * mmToDip;

        // Clamp to page bounds
        srcX = Math.Max(0, Math.Min(srcX, page.Size.Width));
        srcY = Math.Max(0, Math.Min(srcY, page.Size.Height));
        srcW = Math.Max(1, Math.Min(srcW, page.Size.Width - srcX));
        srcH = Math.Max(1, Math.Min(srcH, page.Size.Height - srcY));

        var scale = dpi / 96.0;
        var options = new PdfPageRenderOptions
        {
            SourceRect = new Windows.Foundation.Rect(srcX, srcY, srcW, srcH),
            DestinationWidth = (uint)Math.Max(1, srcW * scale),
            DestinationHeight = (uint)Math.Max(1, srcH * scale),
            BackgroundColor = new Windows.UI.Color { A = 255, R = 255, G = 255, B = 255 }
        };

        await page.RenderToStreamAsync(stream, options);
        stream.Seek(0);

        using var reader = new DataReader(stream);
        await reader.LoadAsync((uint)stream.Size);
        var bytes = new byte[stream.Size];
        reader.ReadBytes(bytes);

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.StreamSource = new MemoryStream(bytes);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        stream.Dispose();
        return bitmap;
    }

    /// <summary>Gets the page dimensions in millimeters.</summary>
    public (double WidthMm, double HeightMm) GetPageSizeMm(int pageIndex)
    {
        if (_pdfDocument == null)
            throw new InvalidOperationException("PDF未加载");

        using var page = _pdfDocument.GetPage((uint)pageIndex);
        // 1 DIP = 1/96 inch, 1 inch = 25.4mm
        return (page.Size.Width / 96.0 * 25.4, page.Size.Height / 96.0 * 25.4);
    }

    /// <summary>Gets the page dimensions in DIPs (96 per inch).</summary>
    public (double Width, double Height) GetPageSizeDip(int pageIndex)
    {
        if (_pdfDocument == null)
            throw new InvalidOperationException("PDF未加载");

        using var page = _pdfDocument.GetPage((uint)pageIndex);
        return (page.Size.Width, page.Size.Height);
    }

    /// <summary>
    /// Detects the content bounding box of a PDF page by trimming surrounding whitespace.
    /// Renders at a low DPI, scans pixels to find non-white content, and returns the
    /// bounding box in millimeters. A small padding is added so content is not clipped
    /// right at the edge.
    /// </summary>
    public async Task<(double X, double Y, double Width, double Height)> GetContentBoundsMmAsync(
        int pageIndex, int threshold = 245, double paddingMm = 0.5)
    {
        var (pageWidthMm, pageHeightMm) = GetPageSizeMm(pageIndex);

        // Render at low DPI for fast whitespace detection
        const double detectionDpi = 72;
        var bitmap = await RenderPageAsync(pageIndex, detectionDpi);

        // Convert to Bgra32 for consistent pixel access
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        int w = converted.PixelWidth;
        int h = converted.PixelHeight;
        int stride = w * 4;
        var pixels = new byte[stride * h];
        converted.CopyPixels(pixels, stride, 0);

        // Find bounding box of non-white pixels
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            int rowOffset = y * stride;
            for (int x = 0; x < w; x++)
            {
                int idx = rowOffset + x * 4;
                byte b = pixels[idx];
                byte g = pixels[idx + 1];
                byte r = pixels[idx + 2];
                // Pixel is considered "content" if any channel is below threshold
                if (r < threshold || g < threshold || b < threshold)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }
        }

        // If no content found, return full page
        if (maxX < 0)
            return (0, 0, pageWidthMm, pageHeightMm);

        // Convert pixel bounds to mm (at detectionDpi, 1 pixel = 25.4/detectionDpi mm)
        double pixelToMm = 25.4 / detectionDpi;
        double contentX = Math.Max(0, minX * pixelToMm - paddingMm);
        double contentY = Math.Max(0, minY * pixelToMm - paddingMm);
        double contentRight = Math.Min(pageWidthMm, (maxX + 1) * pixelToMm + paddingMm);
        double contentBottom = Math.Min(pageHeightMm, (maxY + 1) * pixelToMm + paddingMm);

        return (contentX, contentY, contentRight - contentX, contentBottom - contentY);
    }

    /// <summary>Releases the loaded document.</summary>
    public void Close()
    {
        _pdfDocument = null;
    }
}
