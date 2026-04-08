using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CodePrint.Models;
using CodePrint.ViewModels;

namespace CodePrint.Views.Panels;

public partial class PropertyPanel : UserControl
{
    public PropertyPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        if (e.NewValue is MainViewModel newVm)
            newVm.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedElement))
        {
            var vm = DataContext as MainViewModel;
            BuildElementProperties(vm?.SelectedElement);
        }
    }

    private void BuildElementProperties(LabelElement? element)
    {
        ElementPropertiesPanel.Children.Clear();
        if (element == null) return;

        switch (element)
        {
            case TextElement text:
                AddPropertyField("内容", text, nameof(TextElement.Content), true);
                AddPropertyField("字体", text, nameof(TextElement.FontFamily));
                AddPropertyField("字号", text, nameof(TextElement.FontSize));
                AddCheckBox("粗体", text, nameof(TextElement.IsBold));
                AddCheckBox("斜体", text, nameof(TextElement.IsItalic));
                AddCheckBox("下划线", text, nameof(TextElement.IsUnderline));
                AddCheckBox("删除线", text, nameof(TextElement.IsStrikethrough));
                AddAlignmentButtons("对齐方式", text);
                AddComboBox("文字方向", text, nameof(TextElement.Direction),
                    new[] { "Horizontal", "Vertical" });
                AddPropertyField("文字颜色", text, nameof(TextElement.ForegroundColor));
                AddPropertyField("背景颜色", text, nameof(TextElement.BackgroundColor));
                AddPropertyField("字间距", text, nameof(TextElement.LetterSpacing));
                AddPropertyField("行间距", text, nameof(TextElement.LineSpacing));
                AddCheckBox("多行文本", text, nameof(TextElement.IsMultiline));
                break;

            case BarcodeElement barcode:
                AddPropertyField("条码内容", barcode, nameof(BarcodeElement.Content));
                AddComboBox("条码格式", barcode, nameof(BarcodeElement.Format),
                    Enum.GetNames<BarcodeFormat>());
                AddCheckBox("显示文字", barcode, nameof(BarcodeElement.ShowText));
                AddCheckBox("文字在下方", barcode, nameof(BarcodeElement.TextOnBottom));
                AddPropertyField("条码颜色", barcode, nameof(BarcodeElement.ForegroundColor));
                AddPropertyField("背景颜色", barcode, nameof(BarcodeElement.BackgroundColor));
                AddPropertyField("条码高度(mm)", barcode, nameof(BarcodeElement.BarHeight));
                break;

            case LinkedQrCodeElement linkedQr:
                AddPropertyField("二维码内容", linkedQr, nameof(QrCodeElement.Content));
                AddComboBox("内容类型", linkedQr, nameof(QrCodeElement.ContentType),
                    Enum.GetNames<QrContentType>());
                AddComboBox("纠错级别", linkedQr, nameof(QrCodeElement.ErrorCorrection),
                    Enum.GetNames<QrErrorCorrectionLevel>());
                AddPropertyField("码色", linkedQr, nameof(QrCodeElement.ForegroundColor));
                AddPropertyField("背景色", linkedQr, nameof(QrCodeElement.BackgroundColor));
                AddPropertyField("Logo路径", linkedQr, nameof(QrCodeElement.LogoPath));
                AddPropertyField("关联URL", linkedQr, nameof(LinkedQrCodeElement.LinkedUrl));
                AddCheckBox("动态内容", linkedQr, nameof(LinkedQrCodeElement.IsDynamic));
                break;

            case QrCodeElement qr:
                AddPropertyField("二维码内容", qr, nameof(QrCodeElement.Content));
                AddComboBox("内容类型", qr, nameof(QrCodeElement.ContentType),
                    Enum.GetNames<QrContentType>());
                AddComboBox("纠错级别", qr, nameof(QrCodeElement.ErrorCorrection),
                    Enum.GetNames<QrErrorCorrectionLevel>());
                AddPropertyField("码色", qr, nameof(QrCodeElement.ForegroundColor));
                AddPropertyField("背景色", qr, nameof(QrCodeElement.BackgroundColor));
                AddPropertyField("Logo路径", qr, nameof(QrCodeElement.LogoPath));
                break;

            case ImageElement img:
                AddPropertyField("图片路径", img, nameof(ImageElement.ImagePath));
                AddCheckBox("保持宽高比", img, nameof(ImageElement.MaintainAspectRatio));
                AddPropertyField("圆角", img, nameof(ImageElement.CornerRadius));
                AddPropertyField("亮度", img, nameof(ImageElement.Brightness));
                AddPropertyField("对比度", img, nameof(ImageElement.Contrast));
                break;

            case IconElement icon:
                AddPropertyField("图标", icon, nameof(IconElement.IconKey));
                AddPropertyField("颜色", icon, nameof(IconElement.Color));
                break;

            case LineElement line:
                AddPropertyField("线条颜色", line, nameof(LineElement.StrokeColor));
                AddPropertyField("线条粗细", line, nameof(LineElement.StrokeThickness));
                AddComboBox("线条样式", line, nameof(LineElement.Style),
                    new[] { "Solid", "Dashed", "Dotted" });
                AddComboBox("起点样式", line, nameof(LineElement.StartCap),
                    new[] { "Flat", "Round", "Arrow" });
                AddComboBox("终点样式", line, nameof(LineElement.EndCap),
                    new[] { "Flat", "Round", "Arrow" });
                break;

            case RectangleElement rect:
                AddPropertyField("填充颜色", rect, nameof(RectangleElement.FillColor));
                AddPropertyField("边框颜色", rect, nameof(RectangleElement.BorderColor));
                AddPropertyField("边框粗细", rect, nameof(RectangleElement.BorderThickness));
                AddPropertyField("圆角半径", rect, nameof(RectangleElement.CornerRadius));
                AddCheckBox("阴影", rect, nameof(RectangleElement.HasShadow));
                break;

            case DateElement date:
                AddPropertyField("日期格式", date, nameof(DateElement.DateFormat));
                AddPropertyField("日期偏移(天)", date, nameof(DateElement.DayOffset));
                AddPropertyField("字体", date, nameof(DateElement.FontFamily));
                AddPropertyField("字号", date, nameof(DateElement.FontSize));
                AddPropertyField("文字颜色", date, nameof(DateElement.ForegroundColor));
                break;

            case TableElement table:
                AddPropertyField("行数", table, nameof(TableElement.Rows));
                AddPropertyField("列数", table, nameof(TableElement.Columns));
                AddPropertyField("边框颜色", table, nameof(TableElement.BorderColor));
                AddPropertyField("边框粗细", table, nameof(TableElement.BorderThickness));
                break;

            case PdfElement pdf:
                AddPropertyField("PDF文件路径", pdf, nameof(PdfElement.FilePath));
                AddPropertyField("页码", pdf, nameof(PdfElement.PageNumber));
                break;

            case WarningElement warning:
                AddPropertyField("警示文字", warning, nameof(WarningElement.WarningText));
                AddPropertyField("图标", warning, nameof(WarningElement.IconKey));
                AddCheckBox("显示图标", warning, nameof(WarningElement.ShowIcon));
                AddCheckBox("显示文字", warning, nameof(WarningElement.ShowText));
                break;

            case WatermarkElement watermark:
                AddPropertyField("水印文字", watermark, nameof(WatermarkElement.Text));
                AddPropertyField("字体", watermark, nameof(WatermarkElement.FontFamily));
                AddPropertyField("字号", watermark, nameof(WatermarkElement.FontSize));
                AddPropertyField("颜色", watermark, nameof(WatermarkElement.Color));
                AddPropertyField("角度", watermark, nameof(WatermarkElement.Angle));
                AddPropertyField("间距", watermark, nameof(WatermarkElement.Spacing));
                break;
        }
    }

    private void AddPropertyField(string label, object source, string propertyName, bool isMultiline = false)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var textBox = new TextBox
        {
            Padding = new Thickness(4),
            FontSize = 12,
            BorderBrush = System.Windows.Media.Brushes.LightGray,
            BorderThickness = new Thickness(1)
        };

        if (isMultiline)
        {
            textBox.AcceptsReturn = true;
            textBox.TextWrapping = TextWrapping.Wrap;
            textBox.MinHeight = 50;
        }

        var binding = new Binding(propertyName)
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
        };
        textBox.SetBinding(TextBox.TextProperty, binding);

        stack.Children.Add(textBox);
        ElementPropertiesPanel.Children.Add(stack);
    }

    private void AddCheckBox(string label, object source, string propertyName)
    {
        var cb = new CheckBox
        {
            Content = label,
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 6)
        };

        var binding = new Binding(propertyName)
        {
            Source = source,
            Mode = BindingMode.TwoWay
        };
        cb.SetBinding(CheckBox.IsCheckedProperty, binding);

        ElementPropertiesPanel.Children.Add(cb);
    }

    private void AddAlignmentButtons(string label, TextElement source)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var alignments = new[]
        {
            (Models.TextAlignment.Left, "≡ 左对齐"),
            (Models.TextAlignment.Center, "≡ 居中"),
            (Models.TextAlignment.Right, "≡ 右对齐"),
            (Models.TextAlignment.Justify, "≡ 两端")
        };

        var buttons = new List<Button>();

        foreach (var (alignment, text) in alignments)
        {
            var btn = new Button
            {
                Content = text,
                Style = (Style)FindResource("SmallActionButton"),
                Margin = new Thickness(0, 0, 4, 0),
                Tag = alignment
            };

            UpdateAlignmentButtonAppearance(btn, source.TextAlignment == alignment);

            var capturedAlignment = alignment;
            btn.Click += (s, e) =>
            {
                source.TextAlignment = capturedAlignment;

                // Align element relative to the canvas (label document)
                if (DataContext is MainViewModel vm)
                {
                    var canvasWidth = vm.CurrentDocument.WidthMm;
                    source.X = 0;
                    source.Width = canvasWidth;
                }

                foreach (var b in buttons)
                    UpdateAlignmentButtonAppearance(b, (Models.TextAlignment)b.Tag == capturedAlignment);
            };

            buttons.Add(btn);
            buttonPanel.Children.Add(btn);
        }

        stack.Children.Add(buttonPanel);
        ElementPropertiesPanel.Children.Add(stack);
    }

    private static void UpdateAlignmentButtonAppearance(Button button, bool isActive)
    {
        if (isActive)
        {
            button.Background = (System.Windows.Media.Brush)Application.Current.Resources["PrimaryBrush"];
            button.Foreground = System.Windows.Media.Brushes.White;
        }
        else
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
            button.Foreground = (System.Windows.Media.Brush)Application.Current.Resources["TextPrimaryBrush"];
        }
    }

    private void AddComboBox(string label, object source, string propertyName, string[] options)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 6) };
        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 2)
        });

        var combo = new ComboBox
        {
            FontSize = 12
        };

        foreach (var opt in options)
            combo.Items.Add(opt);

        var binding = new Binding(propertyName)
        {
            Source = source,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };
        combo.SetBinding(ComboBox.SelectedItemProperty, binding);

        stack.Children.Add(combo);
        ElementPropertiesPanel.Children.Add(stack);
    }
}
