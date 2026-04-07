using System.Globalization;
using System.Windows.Data;
using CodePrint.Models;

namespace CodePrint.Converters;

public class ElementTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ElementType type)
        {
            return type switch
            {
                ElementType.Text => "✏",
                ElementType.Barcode => "|||",
                ElementType.QrCode => "⊞",
                ElementType.LinkedQrCode => "🔗",
                ElementType.Image => "🖼",
                ElementType.Icon => "★",
                ElementType.Line => "━",
                ElementType.Rectangle => "▭",
                ElementType.Date => "📅",
                ElementType.Table => "▦",
                ElementType.Pdf => "📄",
                ElementType.Warning => "⚠",
                ElementType.Watermark => "💧",
                _ => "?"
            };
        }
        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
