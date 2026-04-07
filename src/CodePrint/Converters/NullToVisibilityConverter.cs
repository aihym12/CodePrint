using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CodePrint.Converters;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s == "Invert";
        bool isNull = value == null;
        // Default: Visible when not null, Collapsed when null
        // Invert: Visible when null, Collapsed when not null
        bool shouldCollapse = invert ? !isNull : isNull;
        return shouldCollapse ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
