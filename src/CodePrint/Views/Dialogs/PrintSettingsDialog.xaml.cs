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

        // Restore print range radio buttons from settings
        switch (ViewModel.Settings.Range)
        {
            case PrintRange.CurrentPage:
                RangeCurrentRadio.IsChecked = true;
                break;
            case PrintRange.Custom:
                RangeCustomRadio.IsChecked = true;
                break;
            default:
                RangeAllRadio.IsChecked = true;
                break;
        }

        // Restore color mode radio buttons from settings
        switch (ViewModel.Settings.ColorMode)
        {
            case ColorMode.BlackAndWhite:
                ColorModeBWRadio.IsChecked = true;
                break;
            case ColorMode.Grayscale:
                ColorModeGrayRadio.IsChecked = true;
                break;
            default:
                ColorModeColorRadio.IsChecked = true;
                break;
        }
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

    private void RangeAll_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.Range = PrintRange.All;
    }

    private void RangeCurrent_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.Range = PrintRange.CurrentPage;
    }

    private void RangeCustom_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.Range = PrintRange.Custom;
    }

    private void ColorModeColor_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.ColorMode = ColorMode.Color;
    }

    private void ColorModeBW_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.ColorMode = ColorMode.BlackAndWhite;
    }

    private void ColorModeGray_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrintViewModel vm)
            vm.Settings.ColorMode = ColorMode.Grayscale;
    }

    private void OnRequestClose(bool result)
    {
        DialogResult = result;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.SaveSettings();
        Loaded -= OnLoaded;
        ViewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
