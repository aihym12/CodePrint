using System.Windows;
using CodePrint.Views.Dialogs;

namespace CodePrint;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LabelSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LabelSettingsDialog { Owner = this };
        dialog.ShowDialog();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintSettingsDialog { Owner = this };
        dialog.ShowDialog();
    }
}