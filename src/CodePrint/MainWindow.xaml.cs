using System.Windows;
using CodePrint.Services;
using CodePrint.ViewModels;
using CodePrint.Views.Dialogs;
using Microsoft.Win32;

namespace CodePrint;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

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

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileService.FileFilter,
            DefaultExt = FileService.FileExtension,
            FileName = ViewModel.CurrentDocument.Name
        };

        if (dialog.ShowDialog() == true)
        {
            ViewModel.SaveToFile(dialog.FileName);
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PreviewDialog(ViewModel.CurrentDocument) { Owner = this };
        dialog.ShowDialog();
    }
}