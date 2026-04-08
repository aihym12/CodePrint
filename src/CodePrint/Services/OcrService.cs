using System.IO;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace CodePrint.Services;

/// <summary>
/// Recognizes text from images using the Windows OCR API and returns
/// line-level results with bounding-box information.
/// </summary>
public static class OcrService
{
    /// <summary>
    /// Represents a single recognized line of text together with its
    /// bounding rectangle in the image (in pixels).
    /// </summary>
    public class OcrTextLine
    {
        public string Text { get; set; } = string.Empty;

        /// <summary>Bounding box left in image pixels.</summary>
        public double X { get; set; }

        /// <summary>Bounding box top in image pixels.</summary>
        public double Y { get; set; }

        /// <summary>Bounding box width in image pixels.</summary>
        public double Width { get; set; }

        /// <summary>Bounding box height in image pixels.</summary>
        public double Height { get; set; }

        /// <summary>
        /// Median height of individual words in this line (in image pixels).
        /// This provides a tighter estimate of the actual character size
        /// compared to the full line bounding-box height.
        /// </summary>
        public double MedianWordHeight { get; set; }
    }

    /// <summary>
    /// Runs OCR on the specified image file and returns the recognized lines.
    /// </summary>
    /// <param name="imagePath">Absolute path to a PNG, JPEG, or BMP file.</param>
    /// <returns>
    /// A tuple containing (lines, imageWidthPx, imageHeightPx).
    /// </returns>
    public static async Task<(List<OcrTextLine> Lines, double ImageWidth, double ImageHeight)> RecognizeAsync(string imagePath)
    {
        var fullPath = Path.GetFullPath(imagePath);
        var file = await StorageFile.GetFileFromPathAsync(fullPath);

        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        double imgWidth = bitmap.PixelWidth;
        double imgHeight = bitmap.PixelHeight;

        // Prefer Simplified Chinese, fall back to first available language
        var engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("zh-Hans-CN"))
                  ?? OcrEngine.TryCreateFromUserProfileLanguages();

        if (engine == null)
            throw new InvalidOperationException("系统未安装可用的 OCR 语言包，请在 Windows 设置中添加语言包。");

        var result = await engine.RecognizeAsync(bitmap);

        var lines = new List<OcrTextLine>();
        foreach (var ocrLine in result.Lines)
        {
            if (ocrLine.Words.Count == 0)
                continue;

            // Compute the bounding box that encloses all words in this line
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            var wordHeights = new List<double>();

            foreach (var word in ocrLine.Words)
            {
                var r = word.BoundingRect;
                if (r.X < minX) minX = r.X;
                if (r.Y < minY) minY = r.Y;
                if (r.X + r.Width > maxX) maxX = r.X + r.Width;
                if (r.Y + r.Height > maxY) maxY = r.Y + r.Height;
                wordHeights.Add(r.Height);
            }

            // Use median word height for a more stable font-size estimate.
            wordHeights.Sort();
            double medianWordHeight = wordHeights[wordHeights.Count / 2];

            lines.Add(new OcrTextLine
            {
                Text = ocrLine.Text,
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY,
                MedianWordHeight = medianWordHeight
            });
        }

        return (lines, imgWidth, imgHeight);
    }
}
