using System.Globalization;
using System.Windows.Data;

namespace CodePrint.Converters;

/// <summary>
/// Converts millimeters to WPF device-independent pixels (96 DPI).
/// 1 mm = 96/25.4 ≈ 3.7795 pixels
/// </summary>
public class MmToPixelConverter : IValueConverter
{
    public const double MmToPixelFactor = 96.0 / 25.4;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double mm)
        {
            double scale = parameter is double s ? s : 1.0;
            return mm * MmToPixelFactor * scale;
        }
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double px)
        {
            double scale = parameter is double s ? s : 1.0;
            return px / (MmToPixelFactor * scale);
        }
        return 0.0;
    }
}
