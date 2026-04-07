using System.Windows;
using CodePrint.Models;

namespace CodePrint.Views.Dialogs;

public partial class NewLabelDialog : Window
{
    private static int _labelCounter;

    public LabelDocument CreatedDocument { get; private set; } = new();

    public NewLabelDialog()
    {
        InitializeComponent();
        _labelCounter++;
        NameTextBox.Text = $"新建标签_{_labelCounter}";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(WidthTextBox.Text, out var width) || width <= 0)
        {
            MessageBox.Show("请输入有效的宽度", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!double.TryParse(HeightTextBox.Text, out var height) || height <= 0)
        {
            MessageBox.Show("请输入有效的高度", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CreatedDocument = new LabelDocument
        {
            Name = string.IsNullOrWhiteSpace(NameTextBox.Text) ? $"新建标签_{_labelCounter}" : NameTextBox.Text.Trim(),
            WidthMm = width,
            HeightMm = height,
            Orientation = LandscapeRadio.IsChecked == true ? PrintOrientation.Landscape : PrintOrientation.Portrait
        };

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
