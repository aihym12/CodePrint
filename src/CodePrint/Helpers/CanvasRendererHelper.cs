using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CodePrint.Models;

namespace CodePrint.Helpers;

/// <summary>
/// Renders LabelElement instances onto a WPF Canvas.
/// </summary>
public static class CanvasRendererHelper
{
    private static readonly double MmToPx = DesignConstants.MmToPixel;

    /// <summary>
    /// Renders a single element onto the canvas.
    /// </summary>
    public static FrameworkElement RenderElement(Canvas canvas, LabelElement element)
    {
        FrameworkElement visual = element switch
        {
            TextElement text => RenderText(text),
            BarcodeElement barcode => RenderBarcode(barcode),
            QrCodeElement qr => RenderQrCode(qr),
            ImageElement img => RenderImage(img),
            IconElement icon => RenderIcon(icon),
            LineElement line => RenderLine(line),
            RectangleElement rect => RenderRectangle(rect),
            DateElement date => RenderDate(date),
            TableElement table => RenderTable(table),
            PdfElement pdf => RenderPdf(pdf),
            WarningElement warning => RenderWarning(warning),
            WatermarkElement watermark => RenderWatermark(watermark),
            _ => RenderDefault(element)
        };

        Canvas.SetLeft(visual, element.X * MmToPx);
        Canvas.SetTop(visual, element.Y * MmToPx);
        Panel.SetZIndex(visual, element.ZIndex);

        if (element.Rotation != 0)
        {
            visual.RenderTransformOrigin = new Point(0.5, 0.5);
            visual.RenderTransform = new RotateTransform(element.Rotation);
        }

        visual.Opacity = element.Opacity;
        visual.Tag = element.Id;

        canvas.Children.Add(visual);
        return visual;
    }

    private static FrameworkElement RenderText(TextElement element)
    {
        var tb = new TextBlock
        {
            Text = element.Content,
            FontFamily = new FontFamily(element.FontFamily),
            FontSize = element.FontSize,
            FontWeight = element.IsBold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = element.IsItalic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = BrushFromHex(element.ForegroundColor),
            Width = element.Width * MmToPx,
            TextWrapping = element.IsMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
            TextAlignment = element.TextAlignment switch
            {
                Models.TextAlignment.Left => System.Windows.TextAlignment.Left,
                Models.TextAlignment.Center => System.Windows.TextAlignment.Center,
                Models.TextAlignment.Right => System.Windows.TextAlignment.Right,
                Models.TextAlignment.Justify => System.Windows.TextAlignment.Justify,
                _ => System.Windows.TextAlignment.Left
            }
        };

        // Apply line spacing via LineHeight
        if (element.LineSpacing > 0)
        {
            tb.LineHeight = element.FontSize * element.LineSpacing;
            tb.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;
        }

        // Apply character spacing via per-character horizontal offsets
        if (element.LetterSpacing != 0 && !string.IsNullOrEmpty(element.Content))
        {
            var textEffects = new TextEffectCollection();
            for (int i = 1; i < element.Content.Length; i++)
            {
                textEffects.Add(new TextEffect
                {
                    PositionStart = i,
                    PositionCount = 1,
                    Transform = new TranslateTransform(element.LetterSpacing * i, 0)
                });
            }
            tb.TextEffects = textEffects;
        }

        if (element.IsUnderline)
            tb.TextDecorations = TextDecorations.Underline;
        if (element.IsStrikethrough)
            tb.TextDecorations = TextDecorations.Strikethrough;

        var border = new Border
        {
            Child = tb,
            Background = element.BackgroundColor == "Transparent"
                ? Brushes.Transparent
                : BrushFromHex(element.BackgroundColor),
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx
        };

        return border;
    }

    private static FrameworkElement RenderBarcode(BarcodeElement element)
    {
        var border = new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            BorderBrush = BrushFromHex(element.ForegroundColor),
            BorderThickness = new Thickness(1),
            Background = BrushFromHex(element.BackgroundColor)
        };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Barcode visual representation (vertical lines)
        var barcodeVisual = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        var rng = new Random(element.Content.GetHashCode());
        for (int i = 0; i < 30; i++)
        {
            barcodeVisual.Children.Add(new System.Windows.Shapes.Rectangle
            {
                Width = rng.Next(1, 4),
                Height = element.BarHeight > 0 ? element.BarHeight * MmToPx * 0.3 : element.Height * MmToPx * 0.5,
                Fill = i % 2 == 0 ? BrushFromHex(element.ForegroundColor) : Brushes.Transparent,
                Margin = new Thickness(0)
            });
        }
        stack.Children.Add(barcodeVisual);

        if (element.ShowText)
        {
            stack.Children.Add(new TextBlock
            {
                Text = element.Content,
                FontSize = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = BrushFromHex(element.ForegroundColor)
            });
        }

        border.Child = stack;
        return border;
    }

    private static FrameworkElement RenderQrCode(QrCodeElement element)
    {
        var size = Math.Min(element.Width, element.Height) * MmToPx;

        var border = new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Background = BrushFromHex(element.BackgroundColor)
        };

        // Simple QR code placeholder grid
        var grid = new Grid
        {
            Width = size * 0.8,
            Height = size * 0.8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var rng = new Random(element.Content.GetHashCode());
        int cells = 7;
        for (int i = 0; i < cells; i++) grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (int i = 0; i < cells; i++) grid.RowDefinitions.Add(new RowDefinition());

        for (int r = 0; r < cells; r++)
        {
            for (int c = 0; c < cells; c++)
            {
                var rect = new System.Windows.Shapes.Rectangle
                {
                    Fill = rng.Next(2) == 0 ? BrushFromHex(element.ForegroundColor) : Brushes.Transparent
                };
                Grid.SetRow(rect, r);
                Grid.SetColumn(rect, c);
                grid.Children.Add(rect);
            }
        }

        border.Child = grid;
        return border;
    }

    private static FrameworkElement RenderImage(ImageElement element)
    {
        var border = new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            CornerRadius = new CornerRadius(element.CornerRadius),
            Background = Brushes.LightGray,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5)
        };

        border.Child = new TextBlock
        {
            Text = string.IsNullOrEmpty(element.ImagePath) ? "🖼 图片" : System.IO.Path.GetFileName(element.ImagePath),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.Gray,
            FontSize = 10
        };

        return border;
    }

    private static FrameworkElement RenderIcon(IconElement element)
    {
        return new TextBlock
        {
            Text = element.IconKey ?? "★",
            FontSize = Math.Min(element.Width, element.Height) * MmToPx * 0.7,
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Foreground = BrushFromHex(element.Color),
            TextAlignment = System.Windows.TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static FrameworkElement RenderLine(LineElement element)
    {
        var line = new Line
        {
            X1 = 0,
            Y1 = 0,
            X2 = element.Width * MmToPx,
            Y2 = element.Height * MmToPx,
            Stroke = BrushFromHex(element.StrokeColor),
            StrokeThickness = element.StrokeThickness
        };

        if (element.Style == LineStyle.Dashed)
            line.StrokeDashArray = new DoubleCollection { 4, 2 };
        else if (element.Style == LineStyle.Dotted)
            line.StrokeDashArray = new DoubleCollection { 1, 2 };

        var canvas = new Canvas
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx
        };
        canvas.Children.Add(line);
        return canvas;
    }

    private static FrameworkElement RenderRectangle(RectangleElement element)
    {
        return new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Background = element.FillColor == "Transparent"
                ? Brushes.Transparent
                : BrushFromHex(element.FillColor),
            BorderBrush = BrushFromHex(element.BorderColor),
            BorderThickness = new Thickness(element.BorderThickness),
            CornerRadius = new CornerRadius(element.CornerRadius)
        };
    }

    private static FrameworkElement RenderDate(DateElement element)
    {
        var date = DateTime.Now.AddDays(element.DayOffset);
        var formatted = date.ToString(ConvertDateFormat(element.DateFormat));

        return new TextBlock
        {
            Text = formatted,
            FontFamily = new FontFamily(element.FontFamily),
            FontSize = element.FontSize,
            Foreground = BrushFromHex(element.ForegroundColor),
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static FrameworkElement RenderTable(TableElement element)
    {
        var grid = new Grid
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx
        };

        for (int r = 0; r < element.Rows; r++)
            grid.RowDefinitions.Add(new RowDefinition());
        for (int c = 0; c < element.Columns; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        for (int r = 0; r < element.Rows; r++)
        {
            for (int c = 0; c < element.Columns; c++)
            {
                var cell = new Border
                {
                    BorderBrush = BrushFromHex(element.BorderColor),
                    BorderThickness = new Thickness(element.BorderThickness / 2.0)
                };

                var cellData = element.Cells.FirstOrDefault(
                    x => x.Row == r && x.Column == c);
                if (cellData != null)
                {
                    cell.Child = new TextBlock
                    {
                        Text = cellData.Content,
                        FontSize = 8,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(2)
                    };
                    if (!string.IsNullOrEmpty(cellData.BackgroundColor))
                        cell.Background = BrushFromHex(cellData.BackgroundColor);
                }

                Grid.SetRow(cell, r);
                Grid.SetColumn(cell, c);
                grid.Children.Add(cell);
            }
        }

        return new Border
        {
            Child = grid,
            BorderBrush = BrushFromHex(element.BorderColor),
            BorderThickness = new Thickness(element.BorderThickness)
        };
    }

    private static FrameworkElement RenderPdf(PdfElement element)
    {
        return new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Background = Brushes.White,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = $"📄 PDF\n{(string.IsNullOrEmpty(element.FilePath) ? "无文件" : System.IO.Path.GetFileName(element.FilePath))}",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = System.Windows.TextAlignment.Center,
                Foreground = Brushes.Gray,
                FontSize = 10
            }
        };
    }

    private static FrameworkElement RenderWarning(WarningElement element)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (element.ShowIcon)
        {
            stack.Children.Add(new TextBlock
            {
                Text = element.IconKey ?? "⚠",
                FontSize = 16,
                Margin = new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        if (element.ShowText)
        {
            stack.Children.Add(new TextBlock
            {
                Text = element.WarningText,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Background = Brushes.LightYellow,
            BorderBrush = Brushes.Orange,
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static FrameworkElement RenderWatermark(WatermarkElement element)
    {
        var tb = new TextBlock
        {
            Text = element.Text,
            FontFamily = new FontFamily(element.FontFamily),
            FontSize = element.FontSize,
            Foreground = BrushFromHex(element.Color),
            Opacity = element.Opacity,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new RotateTransform(element.Angle)
        };

        return tb;
    }

    private static FrameworkElement RenderDefault(LabelElement element)
    {
        return new Border
        {
            Width = element.Width * MmToPx,
            Height = element.Height * MmToPx,
            Background = Brushes.LightGray,
            BorderBrush = Brushes.Gray,
            BorderThickness = new Thickness(0.5),
            Child = new TextBlock
            {
                Text = element.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10
            }
        };
    }

    private static SolidColorBrush BrushFromHex(string hex)
    {
        try
        {
            if (string.IsNullOrEmpty(hex) || hex == "Transparent")
                return Brushes.Transparent;
            var color = (Color)ColorConverter.ConvertFromString(hex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return Brushes.Black;
        }
    }

    private static string ConvertDateFormat(string format)
    {
        return format
            .Replace("YYYY", "yyyy")
            .Replace("DD", "dd")
            .Replace("MM", "MM");
    }
}
