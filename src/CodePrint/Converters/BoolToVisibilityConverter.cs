using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CodePrint.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            bool invert = parameter is string s && s == "Invert";
            return (b ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility v)
        {
            bool invert = parameter is string s && s == "Invert";
            return (v == Visibility.Visible) ^ invert;
        }
        return false;
    }
}
