using System.Globalization;
using System.Windows.Data;
using CodePrint.Models;

namespace CodePrint.Converters;

/// <summary>将 <see cref="AppLanguage"/> 枚举值转换为友好的显示名称。</summary>
public class LanguageDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AppLanguage lang)
        {
            return lang switch
            {
                AppLanguage.Chinese => "中文",
                AppLanguage.English => "English",
                _ => value.ToString()!
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
