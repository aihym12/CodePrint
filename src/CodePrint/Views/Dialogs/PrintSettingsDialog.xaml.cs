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

        // Restore orientation radio buttons from settings
        if (ViewModel.Settings.Orientation == PrintOrientation.Landscape)
            LandscapeRadio.IsChecked = true;
        else
            PortraitRadio.IsChecked = true;
    }

    private void Landscape_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.Orientation = PrintOrientation.Landscape;
    }

    private void Portrait_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.Orientation = PrintOrientation.Portrait;
    }

    private void OnRequestClose(bool result)
    {
        DialogResult = result;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        Loaded -= OnLoaded;
        ViewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
