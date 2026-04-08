using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodePrint.Helpers;
using CodePrint.Models;

namespace CodePrint.Converters;

/// <summary>
/// Converts a LabelDocument into a thumbnail ImageSource by rendering its elements onto a canvas.
/// </summary>
public class LabelThumbnailConverter : IValueConverter
{
    private static readonly double MmToPx = DesignConstants.MmToPixel;

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not LabelDocument doc || doc.Elements.Count == 0)
            return null;

        try
        {
            double docWidth = doc.WidthMm * MmToPx;
            double docHeight = doc.HeightMm * MmToPx;

            if (docWidth <= 0 || docHeight <= 0)
                return null;

            var canvas = new Canvas
            {
                Width = docWidth,
                Height = docHeight,
                Background = doc.BackgroundColor == "Transparent"
                    ? Brushes.White
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString(doc.BackgroundColor))
            };

            foreach (var element in doc.Elements.OrderBy(e => e.ZIndex))
            {
                if (!element.IsVisible) continue;
                CanvasRendererHelper.RenderElement(canvas, element);
            }

            canvas.Measure(new Size(docWidth, docHeight));
            canvas.Arrange(new Rect(0, 0, docWidth, docHeight));
            canvas.UpdateLayout();

            int bitmapWidth = (int)Math.Max(1, docWidth);
            int bitmapHeight = (int)Math.Max(1, docHeight);
            var renderBitmap = new RenderTargetBitmap(
                bitmapWidth, bitmapHeight, 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(canvas);
            renderBitmap.Freeze();

            return renderBitmap;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
