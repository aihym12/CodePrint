using System.IO;
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

    /// <summary>Releases the loaded document.</summary>
    public void Close()
    {
        _pdfDocument = null;
    }
}
