using System.Windows;
using CodePrint.Models;
using CodePrint.ViewModels;

namespace CodePrint.Views.Dialogs;

public partial class PrintSettingsDialog : Window
{
    public PrintSettingsDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private PrintViewModel ViewModel => (PrintViewModel)DataContext;

    /// <summary>Sets the document to be printed.</summary>
    public void SetDocument(LabelDocument document)
    {
        ViewModel.Document = document;
        ViewModel.Settings.PaperWidth = document.WidthMm;
        ViewModel.Settings.PaperHeight = document.HeightMm;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.RequestClose += OnRequestClose;
        ViewModel.RefreshPrintersCommand.Execute(null);
    }

    private void OnRequestClose(bool result)
    {
        DialogResult = result;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
