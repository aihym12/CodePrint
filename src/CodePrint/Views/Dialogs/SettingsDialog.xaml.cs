using System.Windows;
using CodePrint.Models;
using CodePrint.ViewModels;

namespace CodePrint.Views.Dialogs;

public partial class SettingsDialog : Window
{
    public SettingsDialog()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadFromSettings();
        ViewModel.RequestClose += OnRequestClose;

        // 同步方向单选按钮
        if (ViewModel.DefaultOrientation == PrintOrientation.Landscape)
            LandscapeRadio.IsChecked = true;
        else
            PortraitRadio.IsChecked = true;
    }

    private void Portrait_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.DefaultOrientation = PrintOrientation.Portrait;
    }

    private void Landscape_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.DefaultOrientation = PrintOrientation.Landscape;
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
