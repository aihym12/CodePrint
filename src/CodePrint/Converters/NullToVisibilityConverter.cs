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
        bool showWhenNull = invert ? !isNull : isNull;
        return showWhenNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
